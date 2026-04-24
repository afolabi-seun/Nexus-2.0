using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Api.Middleware;
using SecurityService.Application.DTOs;
using SecurityService.Domain.Interfaces.Services.Outbox;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for GlobalExceptionHandlerMiddleware outbox failure resilience.
/// Feature: architecture-hardening, Property 10: GlobalExceptionHandler outbox failure resilience
/// **Validates: Requirements 7.6**
/// </summary>
public class GlobalExceptionHandlerResiliencePropertyTests
{
    private static readonly Type[] UnhandledExceptionTypes =
    {
        typeof(InvalidOperationException),
        typeof(NullReferenceException),
        typeof(ArgumentException),
        typeof(TimeoutException),
        typeof(FormatException),
        typeof(NotSupportedException),
        typeof(IndexOutOfRangeException),
        typeof(ApplicationException)
    };

    private static readonly Exception[] OutboxExceptions =
    {
        new InvalidOperationException("Redis connection lost"),
        new TimeoutException("Outbox publish timed out"),
        new IOException("Network error during publish"),
        new OperationCanceledException("Publish cancelled"),
        new NotSupportedException("Outbox not available"),
        new ApplicationException("Outbox internal error"),
        new ArgumentException("Invalid outbox key")
    };

    /// <summary>
    /// For any exception thrown by IOutboxService.PublishAsync during exception handling,
    /// the GlobalExceptionHandlerMiddleware SHALL catch the publish failure, log it locally,
    /// and still return the structured ApiResponse error response to the client.
    /// The ErrorLogged flag SHALL NOT be set since the publish failed.
    /// **Validates: Requirements 7.6**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Middleware_CatchesOutboxFailure_StillReturnsStructuredErrorResponse(ushort seed)
    {
        var rng = new Random(seed);

        // Generate a random unhandled exception to trigger the handler
        var exType = UnhandledExceptionTypes[rng.Next(UnhandledExceptionTypes.Length)];
        var exMessage = GenerateRandomString(rng, 1, 80);

        Exception thrownException;
        try
        {
            thrownException = (Exception)Activator.CreateInstance(exType, exMessage)!;
        }
        catch
        {
            thrownException = new InvalidOperationException(exMessage);
        }

        // Pick a random exception for the outbox to throw
        var outboxException = OutboxExceptions[rng.Next(OutboxExceptions.Length)];

        var correlationId = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid().ToString();

        // Configure IOutboxService.PublishAsync to throw
        var mockOutbox = new Mock<IOutboxService>();
        mockOutbox
            .Setup(o => o.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(outboxException);

        // Track LogError calls for the publish failure
        var logErrorCalled = false;
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        mockLogger
            .Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logErrorCalled = true);

        // _next delegate throws the unhandled exception to trigger the handler
        RequestDelegate next = _ => throw thrownException;

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object);

        // Build HttpContext with service provider
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/test";
        httpContext.Items["CorrelationId"] = correlationId;
        httpContext.Items["TenantId"] = tenantId;

        var services = new ServiceCollection();
        services.AddSingleton(mockOutbox.Object);
        httpContext.RequestServices = services.BuildServiceProvider();

        // Enable response body writing
        httpContext.Response.Body = new MemoryStream();

        // The middleware must NOT throw — it should catch the outbox exception
        try
        {
            middleware.InvokeAsync(httpContext).GetAwaiter().GetResult();
        }
        catch
        {
            // Middleware threw — property violated
            return false;
        }

        // Response status code must be 500
        if (httpContext.Response.StatusCode != 500) return false;

        // Read and verify the response body contains structured ApiResponse
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var responseBody = reader.ReadToEnd();

        if (string.IsNullOrEmpty(responseBody)) return false;

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Verify structured ApiResponse with errorCode "INTERNAL_ERROR"
        if (!root.TryGetProperty("errorCode", out var errorCodeProp)) return false;
        if (errorCodeProp.GetString() != "INTERNAL_ERROR") return false;

        if (!root.TryGetProperty("success", out var successProp)) return false;
        if (successProp.GetBoolean() != false) return false;

        // ErrorLogged flag should NOT be set (since publish failed)
        if (httpContext.Items.ContainsKey("ErrorLogged") && httpContext.Items["ErrorLogged"] is true)
            return false;

        // LogError must have been called (for the publish failure)
        if (!logErrorCalled) return false;

        return true;
    }

    private static string GenerateRandomString(Random rng, int minLen, int maxLen)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
        var length = rng.Next(minLen, maxLen + 1);
        return new string(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}
