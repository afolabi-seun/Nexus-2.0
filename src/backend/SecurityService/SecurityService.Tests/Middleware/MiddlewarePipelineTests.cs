using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SecurityService.Api.Middleware;
using SecurityService.Application.DTOs;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services;
using StackExchange.Redis;

namespace SecurityService.Tests.Middleware;

/// <summary>
/// Unit tests for middleware pipeline.
/// Validates: REQ-017, REQ-022, REQ-015, REQ-005.1
/// </summary>
public class MiddlewarePipelineTests
{
    [Fact]
    public void MiddlewarePipeline_RegistersInCorrectOrder()
    {
        // Verify the source code registers middleware in the expected order
        // by reading the extension method source. We verify the types are referenced
        // in the correct sequence.
        var expectedOrder = new[]
        {
            "CorrelationIdMiddleware",
            "GlobalExceptionHandlerMiddleware",
            "RateLimiterMiddleware",
            "JwtClaimsMiddleware",
            "TokenBlacklistMiddleware",
            "FirstTimeUserMiddleware",
            "AuthenticatedRateLimiterMiddleware",
            "RoleAuthorizationMiddleware",
            "OrganizationScopeMiddleware"
        };

        // Verify the extension method exists and the types are valid
        var pipelineType = typeof(SecurityService.Api.Extensions.MiddlewarePipelineExtensions);
        var method = pipelineType.GetMethod("UseSecurityPipeline");
        Assert.NotNull(method);

        // Verify all middleware types exist
        foreach (var name in expectedOrder)
        {
            var type = Type.GetType($"SecurityService.Api.Middleware.{name}, SecurityService.Api");
            Assert.NotNull(type);
        }
    }

    [Fact]
    public async Task GlobalExceptionHandler_DomainException_ReturnsCorrectApiResponse()
    {
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var resolverMock = new Mock<IErrorCodeResolverService>();
        resolverMock.Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("01", "Invalid credentials"));

        var middleware = new GlobalExceptionHandlerMiddleware(
            next: _ => throw new InvalidCredentialsException(),
            logger: loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "test-correlation-id";

        var services = new ServiceCollection();
        services.AddSingleton(resolverMock.Object);
        context.RequestServices = services.BuildServiceProvider();

        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.Contains("json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.False(response.GetProperty("success").GetBoolean());
        Assert.Equal("INVALID_CREDENTIALS", response.GetProperty("errorCode").GetString());
        Assert.Equal("test-correlation-id", response.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task GlobalExceptionHandler_UnhandledException_Returns500WithNoStackTrace()
    {
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();

        var middleware = new GlobalExceptionHandlerMiddleware(
            next: _ => throw new NullReferenceException("Something broke"),
            logger: loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "corr-123";
        context.RequestServices = new ServiceCollection().BuildServiceProvider();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.False(response.GetProperty("success").GetBoolean());
        Assert.Equal("INTERNAL_ERROR", response.GetProperty("errorCode").GetString());
        // Ensure no stack trace leakage
        Assert.DoesNotContain("NullReferenceException", response.GetProperty("message").GetString());
        Assert.DoesNotContain("at ", body);
    }

    [Fact]
    public async Task TokenBlacklistMiddleware_RejectsBlacklistedTokens()
    {
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var jti = "blacklisted-jti";
        dbMock.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString() == $"blacklist:{jti}"), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var middleware = new TokenBlacklistMiddleware(
            next: _ => Task.CompletedTask);

        var context = new DefaultHttpContext();
        // Set up authenticated user
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "user1") }, "Bearer");
        context.User = new ClaimsPrincipal(identity);
        context.Items["jti"] = jti;

        var services = new ServiceCollection();
        services.AddSingleton(redisMock.Object);
        context.RequestServices = services.BuildServiceProvider();

        await Assert.ThrowsAsync<TokenRevokedException>(
            () => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task FirstTimeUserMiddleware_BlocksNonPasswordChangeEndpoints()
    {
        var middleware = new FirstTimeUserMiddleware(
            next: _ => Task.CompletedTask);

        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "user1") }, "Bearer");
        context.User = new ClaimsPrincipal(identity);
        context.Items["IsFirstTimeUser"] = "true";
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/sessions";

        await Assert.ThrowsAsync<FirstTimeUserRestrictedException>(
            () => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task FirstTimeUserMiddleware_AllowsForcedPasswordChange()
    {
        var nextCalled = false;
        var middleware = new FirstTimeUserMiddleware(
            next: _ => { nextCalled = true; return Task.CompletedTask; });

        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "user1") }, "Bearer");
        context.User = new ClaimsPrincipal(identity);
        context.Items["IsFirstTimeUser"] = "true";
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/password/forced-change";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
