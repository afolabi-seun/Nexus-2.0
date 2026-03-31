using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProfileService.Api.Middleware;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Services.ErrorCodeResolver;
using ProfileService.Api.Attributes;

namespace ProfileService.Tests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    [Fact]
    public async Task DomainException_ReturnsCorrectApiResponse()
    {
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "test-corr-id";
        context.Response.Body = new MemoryStream();

        // Set up RequestServices so GetService<IErrorCodeResolverService>() returns null (not throw)
        var services = new ServiceCollection();
        context.RequestServices = services.BuildServiceProvider();

        var middleware = new GlobalExceptionHandlerMiddleware(
            next: _ => throw new OrganizationNameDuplicateException(),
            logger: Mock.Of<ILogger<GlobalExceptionHandlerMiddleware>>());

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        Assert.NotNull(response);
        Assert.False(response!.Success);
        Assert.Equal(ErrorCodes.OrganizationNameDuplicate, response.ErrorCode);
        Assert.Equal("test-corr-id", response.CorrelationId);
    }

    [Fact]
    public async Task UnhandledException_Returns500()
    {
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "test-corr-id";
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlerMiddleware(
            next: _ => throw new InvalidOperationException("Something broke"),
            logger: Mock.Of<ILogger<GlobalExceptionHandlerMiddleware>>());

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(response);
        Assert.False(response!.Success);
        Assert.Equal(ErrorCodes.InternalError, response.ErrorCode);
    }
}

public class OrganizationScopeMiddlewareTests
{
    [Fact]
    public async Task PlatformAdmin_BypassesScopeEnforcement()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin") }, "TestAuth"));
        context.Items["roleName"] = RoleNames.PlatformAdmin;

        var nextCalled = false;
        var middleware = new OrganizationScopeMiddleware(
            next: _ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task ServiceAuthToken_BypassesScopeEnforcement()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "service") }, "TestAuth"));
        context.Items["serviceId"] = "profile-service";

        var nextCalled = false;
        var middleware = new OrganizationScopeMiddleware(
            next: _ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}

public class RoleAuthorizationMiddlewareTests
{
    [Fact]
    public async Task PlatformAdmin_GrantsAccessToPlatformAdminEndpoint()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "admin") }, "TestAuth"));
        context.Items["roleName"] = RoleNames.PlatformAdmin;
        context.Items["CorrelationId"] = "test-corr";

        // Set up endpoint with PlatformAdmin attribute
        var endpoint = new Endpoint(
            requestDelegate: _ => Task.CompletedTask,
            metadata: new EndpointMetadataCollection(new PlatformAdminAttribute()),
            displayName: "TestEndpoint");
        context.SetEndpoint(endpoint);

        var nextCalled = false;
        var middleware = new RoleAuthorizationMiddleware(
            next: _ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
