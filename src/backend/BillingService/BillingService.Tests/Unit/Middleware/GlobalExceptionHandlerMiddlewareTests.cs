using System.Text.Json;
using BillingService.Api.Middleware;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BillingService.Tests.Unit.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static HttpContext CreateContextWithServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IErrorCodeResolverService>().Object);
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext();
        context.RequestServices = provider;
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task DomainException_ReturnsCorrectStatusAndBody()
    {
        var context = CreateContextWithServices();
        context.Items["CorrelationId"] = "corr-123";

        var ex = new PlanNotFoundException();
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw ex,
            new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal("PLAN_NOT_FOUND", json.GetProperty("errorCode").GetString());
        Assert.Equal("corr-123", json.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task UnhandledException_Returns500WithGenericMessage()
    {
        var context = CreateContextWithServices();
        context.Items["CorrelationId"] = "corr-456";

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new NullReferenceException("secret info"),
            new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.DoesNotContain("secret info", body);
        Assert.DoesNotContain("NullReferenceException", body);

        var json = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("INTERNAL_ERROR", json.GetProperty("errorCode").GetString());
        Assert.Equal("corr-456", json.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task CorrelationId_IncludedInResponse()
    {
        var correlationId = Guid.NewGuid().ToString();
        var context = CreateContextWithServices();
        context.Items["CorrelationId"] = correlationId;

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new SubscriptionNotFoundException(),
            new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.Equal(correlationId, json.GetProperty("correlationId").GetString());
    }
}
