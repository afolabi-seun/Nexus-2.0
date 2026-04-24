using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Api.Middleware;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services.ErrorCodeResolver;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Infrastructure.Redis;

namespace SecurityService.Tests.Unit.Middleware;

/// <summary>
/// Unit tests for GlobalExceptionHandlerMiddleware outbox publishing edge cases:
/// RateLimitExceededException exclusion and ErrorLogged flag behavior.
/// Requirements: 7.4, 7.5, 7.6
/// </summary>
public class GlobalExceptionHandlerOutboxTests
{
    private readonly Mock<IOutboxService> _mockOutbox;
    private readonly Mock<IErrorCodeResolverService> _mockResolver;
    private readonly Mock<ILogger<GlobalExceptionHandlerMiddleware>> _mockLogger;

    public GlobalExceptionHandlerOutboxTests()
    {
        _mockOutbox = new Mock<IOutboxService>();
        _mockResolver = new Mock<IErrorCodeResolverService>();
        _mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();

        _mockResolver
            .Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("06", "Some description"));
    }

    /// <summary>
    /// RateLimitExceededException should NOT trigger outbox publish.
    /// Validates: Requirement 7.4
    /// </summary>
    [Fact]
    public async Task RateLimitExceededException_DoesNot_Trigger_OutboxPublish()
    {
        // Arrange
        var rateLimitEx = new RateLimitExceededException(retryAfterSeconds: 60);

        RequestDelegate next = _ => throw rateLimitEx;
        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        var httpContext = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert — PublishAsync should never have been called
        _mockOutbox.Verify(
            o => o.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // ErrorLogged flag should NOT be set
        Assert.False(httpContext.Items.ContainsKey("ErrorLogged"));
    }

    /// <summary>
    /// ErrorLogged flag is set to true after successful outbox publish for a DomainException.
    /// Validates: Requirement 7.5
    /// </summary>
    [Fact]
    public async Task ErrorLogged_Flag_Set_After_Successful_OutboxPublish()
    {
        // Arrange
        _mockOutbox
            .Setup(o => o.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var domainEx = new DomainException(
            errorValue: 2022,
            errorCode: "CONFLICT",
            message: "A conflict occurred.");

        RequestDelegate next = _ => throw domainEx;
        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        var httpContext = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert — ErrorLogged should be true
        Assert.True(httpContext.Items.ContainsKey("ErrorLogged"));
        Assert.Equal(true, httpContext.Items["ErrorLogged"]);

        // Verify publish was called once
        _mockOutbox.Verify(
            o => o.PublishAsync(RedisKeys.Outbox, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// ErrorLogged flag is NOT set when outbox publish fails (throws an exception).
    /// Validates: Requirement 7.6
    /// </summary>
    [Fact]
    public async Task ErrorLogged_Flag_NotSet_When_OutboxPublish_Fails()
    {
        // Arrange — make PublishAsync throw
        _mockOutbox
            .Setup(o => o.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis connection failed"));

        var domainEx = new DomainException(
            errorValue: 2022,
            errorCode: "CONFLICT",
            message: "A conflict occurred.");

        RequestDelegate next = _ => throw domainEx;
        var middleware = new GlobalExceptionHandlerMiddleware(next, _mockLogger.Object);

        var httpContext = CreateHttpContext();

        // Act — should not throw; middleware catches publish failures
        await middleware.InvokeAsync(httpContext);

        // Assert — ErrorLogged should NOT be set since publish failed
        Assert.False(httpContext.Items.ContainsKey("ErrorLogged"));

        // The structured error response should still have been written (status code set)
        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    private DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/test";
        httpContext.Items["CorrelationId"] = Guid.NewGuid().ToString();
        httpContext.Items["TenantId"] = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddSingleton(_mockOutbox.Object);
        services.AddSingleton(_mockResolver.Object);
        httpContext.RequestServices = services.BuildServiceProvider();

        httpContext.Response.Body = new MemoryStream();

        return httpContext;
    }
}
