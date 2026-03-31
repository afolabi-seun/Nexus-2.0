using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using UtilityService.Api.Middleware;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Services.ErrorCodeResolver;

namespace UtilityService.Tests.Middleware;

public class MiddlewareTests
{
    // --- GlobalExceptionHandlerMiddleware ---

    [Fact]
    public async Task GlobalExceptionHandler_DomainException_ReturnsCorrectStatusAndJson()
    {
        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var resolverMock = new Mock<IErrorCodeResolverService>();
        resolverMock.Setup(r => r.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("6002", "Duplicate error code"));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-corr-id";

        var services = new ServiceCollection();
        services.AddSingleton(resolverMock.Object);
        context.RequestServices = services.BuildServiceProvider();

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new ErrorCodeDuplicateException("TEST"),
            logger.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        Assert.Contains("application/", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("ERROR_CODE_DUPLICATE", body);
        Assert.Contains("test-corr-id", body);
    }

    [Fact]
    public async Task GlobalExceptionHandler_UnhandledException_Returns500WithNoStackTrace()
    {
        var logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "unhandled-corr";

        var services = new ServiceCollection();
        context.RequestServices = services.BuildServiceProvider();

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new InvalidOperationException("secret internal details"),
            logger.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Contains("application/", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("INTERNAL_ERROR", body);
        Assert.DoesNotContain("secret internal details", body);
        Assert.Contains("unhandled-corr", body);
    }

    // --- CorrelationIdMiddleware ---

    [Fact]
    public async Task CorrelationIdMiddleware_NoHeader_GeneratesNewId()
    {
        string? capturedId = null;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            capturedId = ctx.Items["CorrelationId"]?.ToString();
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.NotNull(capturedId);
        Assert.NotEmpty(capturedId);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_WithHeader_PropagatesExistingId()
    {
        string? capturedId = null;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            capturedId = ctx.Items["CorrelationId"]?.ToString();
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "my-custom-id";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal("my-custom-id", capturedId);
    }
}
