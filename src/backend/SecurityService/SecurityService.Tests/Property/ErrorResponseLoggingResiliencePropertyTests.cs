using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Api.Middleware;
using SecurityService.Domain.Interfaces.Services.Outbox;

namespace SecurityService.Tests.Property;

/// <summary>
/// Property-based tests for ErrorResponseLoggingMiddleware outbox failure resilience.
/// Feature: architecture-hardening, Property 7: ErrorResponseLoggingMiddleware outbox failure resilience
/// **Validates: Requirements 6.6**
/// </summary>
public class ErrorResponseLoggingResiliencePropertyTests
{
    /// <summary>
    /// For any exception thrown by IOutboxService.PublishAsync during error response logging,
    /// the middleware catches the exception, logs it via ILogger.LogError, and allows the
    /// HTTP response to proceed with the original status code unmodified.
    /// **Validates: Requirements 6.6**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Middleware_CatchesOutboxFailure_LogsError_And_ResponseProceeds(ushort seed)
    {
        var rng = new Random(seed);

        // Generate a random 5xx status code (500–599) to trigger the publish path
        var statusCode = rng.Next(500, 600);

        // Pick a random exception type to throw from PublishAsync
        var exceptions = new Exception[]
        {
            new InvalidOperationException($"Outbox failure seed={seed}"),
            new TimeoutException($"Redis timeout seed={seed}"),
            new IOException($"IO error seed={seed}"),
            new OperationCanceledException($"Cancelled seed={seed}"),
            new ArgumentException($"Bad argument seed={seed}"),
            new NotSupportedException($"Not supported seed={seed}"),
            new ApplicationException($"App error seed={seed}")
        };
        var exceptionToThrow = exceptions[rng.Next(exceptions.Length)];

        // Configure IOutboxService.PublishAsync to throw
        var mockOutbox = new Mock<IOutboxService>();
        mockOutbox
            .Setup(o => o.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exceptionToThrow);

        // Track LogError calls
        var logErrorCalled = false;
        var mockLogger = new Mock<ILogger<ErrorResponseLoggingMiddleware>>();
        mockLogger
            .Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logErrorCalled = true);

        // _next sets the response status code
        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };

        var middleware = new ErrorResponseLoggingMiddleware(next, mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/test";
        httpContext.Items["CorrelationId"] = Guid.NewGuid().ToString();
        httpContext.Items["TenantId"] = Guid.NewGuid().ToString();

        // The middleware must NOT throw — it should catch the outbox exception
        try
        {
            middleware.InvokeAsync(httpContext, mockOutbox.Object).GetAwaiter().GetResult();
        }
        catch
        {
            // Middleware threw — property violated
            return false;
        }

        // Response status code must remain unchanged
        if (httpContext.Response.StatusCode != statusCode) return false;

        // LogError must have been called
        if (!logErrorCalled) return false;

        // PublishAsync was attempted exactly once
        mockOutbox.Verify(
            o => o.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        return true;
    }
}
