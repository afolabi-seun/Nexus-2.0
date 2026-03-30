using System.Net;
using System.Text.Json;
using BillingService.Api.Middleware;
using BillingService.Application.DTOs;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BillingService.Tests.Property;

/// <summary>
/// Property-based tests for error handling.
/// </summary>
public class ErrorHandlingPropertyTests
{
    private static HttpContext CreateContextWithServices(string correlationId)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IErrorCodeResolverService>().Object);
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext();
        context.RequestServices = provider;
        context.Items["CorrelationId"] = correlationId;
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>
    /// Feature: billing-service, Property 28: DomainException returns correct HTTP status and envelope
    /// **Validates: Requirements 13.2**
    /// </summary>
    [Fact]
    public async Task Property28_DomainExceptionReturnsCorrectStatusAndEnvelope()
    {
        var exceptions = new DomainException[]
        {
            new SubscriptionAlreadyExistsException(),
            new PlanNotFoundException(),
            new SubscriptionNotFoundException(),
            new InvalidUpgradePathException(),
            new NoActiveSubscriptionException(),
            new InvalidDowngradePathException(),
            new SubscriptionAlreadyCancelledException(),
            new PaymentProviderException("Stripe error"),
            new InvalidWebhookSignatureException(),
        };

        foreach (var domainEx in exceptions)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var context = CreateContextWithServices(correlationId);

            var middleware = new GlobalExceptionHandlerMiddleware(
                _ => throw domainEx,
                new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object);

            await middleware.InvokeAsync(context);

            Assert.Equal((int)domainEx.StatusCode, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<JsonElement>(body);

            Assert.False(response.GetProperty("success").GetBoolean());
            Assert.Equal(domainEx.ErrorCode, response.GetProperty("errorCode").GetString());
            Assert.Equal(correlationId, response.GetProperty("correlationId").GetString());
        }
    }

    /// <summary>
    /// Feature: billing-service, Property 30: Unhandled exceptions return 500 without internals
    /// **Validates: Requirements 13.5**
    /// </summary>
    [Fact]
    public async Task Property30_UnhandledExceptionsReturn500WithoutInternals()
    {
        var context = CreateContextWithServices("test-correlation");

        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new InvalidOperationException("Secret internal details"),
            new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.DoesNotContain("Secret internal details", body);
        Assert.DoesNotContain("InvalidOperationException", body);

        var response = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.False(response.GetProperty("success").GetBoolean());
        Assert.Equal("INTERNAL_ERROR", response.GetProperty("errorCode").GetString());
    }

    /// <summary>
    /// Feature: billing-service, Property 31: Correlation ID propagation
    /// **Validates: Requirements 14.2**
    /// </summary>
    [Fact]
    public async Task Property31_CorrelationIdPropagation_ExistingHeader()
    {
        var existingId = "my-correlation-id-123";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = existingId;

        string? capturedId = null;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            capturedId = ctx.Items["CorrelationId"]?.ToString();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(existingId, capturedId);
    }

    [Fact]
    public async Task Property31_CorrelationIdPropagation_GeneratesNewWhenAbsent()
    {
        var context = new DefaultHttpContext();

        string? capturedId = null;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            capturedId = ctx.Items["CorrelationId"]?.ToString();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.NotNull(capturedId);
        Assert.NotEmpty(capturedId!);
    }

    /// <summary>
    /// Feature: billing-service, Property 32: OrgAdmin role enforcement
    /// **Validates: Requirements 14.4**
    /// </summary>
    [Fact]
    public async Task Property32_NonOrgAdminRejected()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Simulate authenticated user with non-OrgAdmin role
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim("sub", "user1")], "Bearer");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        context.Items["roleName"] = "Member";
        context.Items["CorrelationId"] = "test";

        // Set up endpoint with OrgAdmin attribute
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new BillingService.Api.Attributes.OrgAdminAttribute()),
            "TestEndpoint");
        context.SetEndpoint(endpoint);

        var middleware = new RoleAuthorizationMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        Assert.Equal(403, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("INSUFFICIENT_PERMISSIONS", body);
    }

    /// <summary>
    /// Feature: billing-service, Property 33: Organization scope validation
    /// **Validates: Requirements 14.5**
    /// </summary>
    [Fact]
    public async Task Property33_OrgScopeMismatchRejected()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim("sub", "user1")], "Bearer");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        context.Items["organizationId"] = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = "test";

        // Query param has different org ID
        context.Request.QueryString = new QueryString($"?organizationId={Guid.NewGuid()}");

        var middleware = new OrganizationScopeMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        Assert.Equal(403, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("ORGANIZATION_MISMATCH", body);
    }

    /// <summary>
    /// Feature: billing-service, Property 38: ApiResponse envelope wraps all responses
    /// **Validates: Requirements 13.1**
    /// </summary>
    [Fact]
    public void Property38_ApiResponseEnvelopeStructure()
    {
        // Success response
        var success = ApiResponse<string>.Ok("test data", "Success");
        Assert.True(success.Success);
        Assert.Equal("00", success.ResponseCode);
        Assert.Equal("test data", success.Data);
        Assert.Null(success.ErrorCode);

        // Failure response
        var fail = ApiResponse<string>.Fail(5001, "SUBSCRIPTION_ALREADY_EXISTS", "Already exists");
        Assert.False(fail.Success);
        Assert.Equal("SUBSCRIPTION_ALREADY_EXISTS", fail.ErrorCode);
        Assert.Equal(5001, fail.ErrorValue);
        Assert.NotNull(fail.ResponseCode);

        // Validation failure
        var validationFail = ApiResponse<string>.ValidationFail("Validation failed",
            [new ErrorDetail { Field = "PlanId", Message = "Required" }]);
        Assert.False(validationFail.Success);
        Assert.Equal("VALIDATION_ERROR", validationFail.ErrorCode);
        Assert.Equal(1000, validationFail.ErrorValue);
        Assert.NotNull(validationFail.Errors);
        Assert.Single(validationFail.Errors!);
    }
}
