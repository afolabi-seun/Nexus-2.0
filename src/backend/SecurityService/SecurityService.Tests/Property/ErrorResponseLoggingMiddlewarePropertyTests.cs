using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Api.Middleware;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Infrastructure.Redis;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for ErrorResponseLoggingMiddleware conditional publish.
/// Feature: architecture-hardening, Property 6: ErrorResponseLoggingMiddleware conditional publish
/// **Validates: Requirements 6.2, 6.3, 6.4**
/// </summary>
public class ErrorResponseLoggingMiddlewarePropertyTests
{
    /// <summary>
    /// For any HTTP status code (100–599) and any ErrorLogged flag state,
    /// the middleware publishes an error log if and only if status >= 500
    /// AND ErrorLogged is not set. When published, the envelope contains
    /// the correct Type, ServiceName, ErrorCode, Severity, CorrelationId, and TenantId.
    /// **Validates: Requirements 6.2, 6.3, 6.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Publish_IfAndOnlyIf_Status500Plus_And_ErrorLoggedNotSet(ushort seed)
    {
        var rng = new Random(seed);

        // Generate random status code 100–599
        var statusCode = rng.Next(100, 600);
        var errorLoggedSet = rng.Next(2) == 0;
        var correlationId = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid().ToString();

        var shouldPublish = statusCode >= 500 && !errorLoggedSet;

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

        var mockLogger = new Mock<ILogger<ErrorResponseLoggingMiddleware>>();

        // _next delegate sets the response status code
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };

        var middleware = new ErrorResponseLoggingMiddleware(next, mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/test";
        httpContext.Items["CorrelationId"] = correlationId;
        httpContext.Items["TenantId"] = tenantId;

        if (errorLoggedSet)
        {
            httpContext.Items["ErrorLogged"] = true;
        }

        middleware.InvokeAsync(httpContext, mockOutbox.Object).GetAwaiter().GetResult();

        if (shouldPublish)
        {
            // Must have published exactly once
            if (publishedJson is null) return false;
            if (publishedKey != RedisKeys.Outbox) return false;

            // Verify envelope fields
            using var doc = JsonDocument.Parse(publishedJson);
            var root = doc.RootElement;

            if (root.GetProperty("Type").GetString() != "error") return false;

            var payload = root.GetProperty("Payload");
            if (payload.GetProperty("ServiceName").GetString() != "SecurityService") return false;
            if (payload.GetProperty("ErrorCode").GetString() != $"HTTP_{statusCode}") return false;
            if (payload.GetProperty("Severity").GetString() != "Error") return false;
            if (payload.GetProperty("CorrelationId").GetString() != correlationId) return false;
            if (payload.GetProperty("TenantId").GetString() != tenantId) return false;

            // Verify Timestamp exists
            if (!root.TryGetProperty("Timestamp", out _)) return false;

            return true;
        }
        else
        {
            // Must NOT have published
            return publishedJson is null;
        }
    }
}
