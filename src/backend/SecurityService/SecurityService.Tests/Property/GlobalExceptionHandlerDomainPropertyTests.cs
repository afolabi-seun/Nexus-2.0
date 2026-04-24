using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Api.Middleware;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services.ErrorCodeResolver;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Infrastructure.Redis;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for GlobalExceptionHandlerMiddleware DomainException outbox publish.
/// Feature: architecture-hardening, Property 8: GlobalExceptionHandler DomainException outbox publish
/// **Validates: Requirements 7.2, 7.5, 7.7**
/// </summary>
public class GlobalExceptionHandlerDomainPropertyTests
{
    /// <summary>
    /// For any DomainException (excluding RateLimitExceededException) with any errorCode,
    /// message, and stackTrace, the GlobalExceptionHandlerMiddleware SHALL publish an error
    /// log to IOutboxService with envelope containing type: "error", the exception's errorCode,
    /// message, stackTrace, correlationId, tenantId, severity: "Warning", and timestamp as UTC.
    /// After publishing, it SHALL set HttpContext.Items["ErrorLogged"] = true.
    /// **Validates: Requirements 7.2, 7.5, 7.7**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool DomainException_Publishes_Warning_Envelope_And_Sets_ErrorLogged(ushort seed)
    {
        var rng = new Random(seed);

        // Generate random DomainException fields
        var errorCode = GenerateRandomString(rng, 3, 30);
        var message = GenerateRandomString(rng, 1, 100);
        var errorValue = rng.Next(1000, 9999);
        var correlationId = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid().ToString();

        // Create a DomainException (not RateLimitExceededException)
        var domainException = new DomainException(errorValue, errorCode, message);

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

        var mockResolver = new Mock<IErrorCodeResolverService>();
        mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((errorCode, message));

        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();

        // _next delegate throws the DomainException
        RequestDelegate next = _ => throw domainException;

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object);

        // Build HttpContext with service provider that resolves IOutboxService and IErrorCodeResolverService
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/test";
        httpContext.Items["CorrelationId"] = correlationId;
        httpContext.Items["TenantId"] = tenantId;

        var services = new ServiceCollection();
        services.AddSingleton(mockOutbox.Object);
        services.AddSingleton(mockResolver.Object);
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
        if (payload.GetProperty("ErrorCode").GetString() != errorCode) return false;
        if (payload.GetProperty("Message").GetString() != message) return false;
        if (payload.GetProperty("Severity").GetString() != "Warning") return false;
        if (payload.GetProperty("CorrelationId").GetString() != correlationId) return false;
        if (payload.GetProperty("TenantId").GetString() != tenantId) return false;

        // StackTrace should be present (may be null for exceptions not thrown with stack)
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
