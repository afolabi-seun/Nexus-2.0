using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WorkService.Api.Middleware;
using WorkService.Application.DTOs;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Services;

namespace WorkService.Tests.Middleware;

public class MiddlewareTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // --- GlobalExceptionHandlerMiddleware ---

    [Fact]
    public async Task GlobalExceptionHandler_DomainException_ReturnsCorrectApiResponse()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-correlation";

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<GlobalExceptionHandlerMiddleware>>(
            Mock.Of<ILogger<GlobalExceptionHandlerMiddleware>>());
        context.RequestServices = services.BuildServiceProvider();

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new StoryNotFoundException(Guid.NewGuid()),
            Mock.Of<ILogger<GlobalExceptionHandlerMiddleware>>());

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(body, JsonOptions);

        Assert.Equal(404, context.Response.StatusCode);
        Assert.NotNull(response);
        Assert.False(response!.Success);
        Assert.Equal(ErrorCodes.StoryNotFound, response.ErrorCode);
        Assert.Equal("test-correlation", response.CorrelationId);
    }

    [Fact]
    public async Task GlobalExceptionHandler_UnhandledException_Returns500()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-500";

        var services = new ServiceCollection();
        context.RequestServices = services.BuildServiceProvider();

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new InvalidOperationException("Something broke"),
            Mock.Of<ILogger<GlobalExceptionHandlerMiddleware>>());

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(body, JsonOptions);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.NotNull(response);
        Assert.False(response!.Success);
        Assert.Equal(ErrorCodes.InternalError, response.ErrorCode);
    }

    // --- CorrelationIdMiddleware ---

    [Fact]
    public async Task CorrelationIdMiddleware_GeneratesId_WhenNotProvided()
    {
        var context = new DefaultHttpContext();
        string? capturedCorrelationId = null;

        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            capturedCorrelationId = ctx.Items["CorrelationId"]?.ToString();
            return System.Threading.Tasks.Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.NotNull(capturedCorrelationId);
        Assert.NotEmpty(capturedCorrelationId!);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_PropagatesExistingId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "my-custom-id";
        string? capturedCorrelationId = null;

        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            capturedCorrelationId = ctx.Items["CorrelationId"]?.ToString();
            return System.Threading.Tasks.Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal("my-custom-id", capturedCorrelationId);
    }
}
