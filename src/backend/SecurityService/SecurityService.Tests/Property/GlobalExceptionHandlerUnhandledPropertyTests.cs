using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Api.Middleware;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Infrastructure.Redis;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for GlobalExceptionHandlerMiddleware unhandled exception outbox publish.
/// Feature: architecture-hardening, Property 9: GlobalExceptionHandler unhandled exception outbox publish
/// **Validates: Requirements 7.3, 7.5, 7.7**
/// </summary>
public class GlobalExceptionHandlerUnhandledPropertyTests
{
    private static readonly Type[] ExceptionTypes =
    {
        typeof(InvalidOperationException),
        typeof(NullReferenceException),
        typeof(ArgumentException),
        typeof(ArgumentNullException),
        typeof(NotSupportedException),
        typeof(TimeoutException),
        typeof(FormatException),
        typeof(IndexOutOfRangeException)
    };

    /// <summary>
    /// For any unhandled exception (not DomainException) with any type, message, and inner exception,
    /// the GlobalExceptionHandlerMiddleware SHALL publish an error log to IOutboxService with envelope
    /// containing errorCode: "INTERNAL_ERROR", a message including the inner exception detail,
    /// severity: "Error", stackTrace, correlationId, tenantId, and timestamp.
    /// After publishing, it SHALL set HttpContext.Items["ErrorLogged"] = true.
    /// **Validates: Requirements 7.3, 7.5, 7.7**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool UnhandledException_Publishes_Error_Envelope_And_Sets_ErrorLogged(ushort seed)
    {
        var rng = new Random(seed);

        // Generate random exception parameters
        var exMessage = GenerateRandomString(rng, 1, 80);
        var hasInnerException = rng.Next(2) == 1;
        var innerMessage = hasInnerException ? GenerateRandomString(rng, 1, 80) : null;
        var correlationId = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid().ToString();

        // Pick a random non-DomainException type
        var exType = ExceptionTypes[rng.Next(ExceptionTypes.Length)];

        // Build the exception with or without inner exception
        Exception innerException = hasInnerException
            ? new Exception(innerMessage)
            : null!;

        Exception thrownException;
        try
        {
            thrownException = hasInnerException
                ? (Exception)Activator.CreateInstance(exType, exMessage, innerException)!
                : (Exception)Activator.CreateInstance(exType, exMessage)!;
        }
        catch
        {
            // Fallback if constructor doesn't match
            thrownException = hasInnerException
                ? new InvalidOperationException(exMessage, innerException)
                : new InvalidOperationException(exMessage);
        }

        var expectedInnerMessage = thrownException.InnerException?.Message ?? thrownException.Message;
        var expectedMessage = $"{thrownException.GetType().Name}: {expectedInnerMessage}";

        // Track published messages
        string? publishedKey = null;
        string? publishedJson = null;

        var mockOutbox = new Mock<IOutboxService>();
        mockOutbox
            .Setup(o => o.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((key, json, _) =>
            {
                publishedKey = key;
                publishedJson = json;
            })
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();

        // _next delegate throws the unhandled exception
        RequestDelegate next = _ => throw thrownException;

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object);

        // Build HttpContext with service provider
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/test";
        httpContext.Items["CorrelationId"] = correlationId;
        httpContext.Items["TenantId"] = tenantId;

        var services = new ServiceCollection();
        services.AddSingleton(mockOutbox.Object);
        httpContext.RequestServices = services.BuildServiceProvider();

        // Enable response body writing
        httpContext.Response.Body = new MemoryStream();

        middleware.InvokeAsync(httpContext).GetAwaiter().GetResult();

        // Verify publish was called
        if (publishedJson is null) return false;
        if (publishedKey != RedisKeys.Outbox) return false;

        // Parse and verify envelope fields
        using var doc = JsonDocument.Parse(publishedJson);
        var root = doc.RootElement;

        if (root.GetProperty("Type").GetString() != "error") return false;

        var payload = root.GetProperty("Payload");
        if (payload.GetProperty("ServiceName").GetString() != "SecurityService") return false;
        if (payload.GetProperty("ErrorCode").GetString() != "INTERNAL_ERROR") return false;
        if (payload.GetProperty("Message").GetString() != expectedMessage) return false;
        if (payload.GetProperty("Severity").GetString() != "Error") return false;
        if (payload.GetProperty("CorrelationId").GetString() != correlationId) return false;
        if (payload.GetProperty("TenantId").GetString() != tenantId) return false;

        // StackTrace should be present
        if (!payload.TryGetProperty("StackTrace", out _)) return false;

        // Verify Timestamp exists
        if (!root.TryGetProperty("Timestamp", out _)) return false;

        // Verify ErrorLogged flag is set
        if (httpContext.Items["ErrorLogged"] is not true) return false;

        return true;
    }

    private static string GenerateRandomString(Random rng, int minLen, int maxLen)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
        var length = rng.Next(minLen, maxLen + 1);
        return new string(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}
