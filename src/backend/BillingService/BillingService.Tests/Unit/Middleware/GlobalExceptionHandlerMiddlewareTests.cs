using System.Text.Json;
using BillingService.Api.Middleware;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Services.ErrorCodeResolver;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

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

    [Fact(Skip = "PostgresException SqlState not settable via public constructor in Npgsql 8.0.6 — requires integration test with real PostgreSQL")]
    public async Task DbUpdateException_UniqueViolation_Returns409WithConstraintName()
    {
        var context = CreateContextWithServices();
        context.Items["CorrelationId"] = "corr-unique";

        // Use 18-param constructor to ensure SqlState and ConstraintName are set
        var pgEx = new PostgresException(
            "ERROR", "ERROR", "23505", "duplicate key value",
            "", "", 0, 0, "", "", "", "plans", "", "", "uq_plans_plan_code", "", "", "");
        var dbEx = new DbUpdateException("Save failed", (Exception)pgEx);

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw dbEx,
            new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(409, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal("UNIQUE_CONSTRAINT_VIOLATION", json.GetProperty("errorCode").GetString());
        Assert.Equal("corr-unique", json.GetProperty("correlationId").GetString());
    }

    [Fact(Skip = "PostgresException SqlState not settable via public constructor in Npgsql 8.0.6 — requires integration test with real PostgreSQL")]
    public async Task DbUpdateException_ForeignKeyViolation_Returns409WithConstraintName()
    {
        var context = CreateContextWithServices();
        context.Items["CorrelationId"] = "corr-fk";

        var pgEx = new PostgresException(
            "ERROR", "ERROR", "23503", "foreign key violation",
            "", "", 0, 0, "", "", "", "subscriptions", "", "", "fk_subscriptions_plan_id", "", "", "");
        var dbEx = new DbUpdateException("Save failed", (Exception)pgEx);

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw dbEx,
            new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(409, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal("FOREIGN_KEY_VIOLATION", json.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task DbUpdateException_NonPostgresInner_Returns500()
    {
        var context = CreateContextWithServices();
        context.Items["CorrelationId"] = "corr-db";

        var dbEx = new DbUpdateException("Save failed", new InvalidOperationException("something else"));

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw dbEx,
            new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.Equal("INTERNAL_ERROR", json.GetProperty("errorCode").GetString());
        Assert.DoesNotContain("something else", body);
    }


}
