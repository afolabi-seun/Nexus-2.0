using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;

namespace WorkService.Tests.Unit.Extensions;

public class ApiResponseExtensionsTests
{
    private static DefaultHttpContext CreateHttpContext(string? correlationId = null)
    {
        var ctx = new DefaultHttpContext();
        if (correlationId is not null)
            ctx.Items["CorrelationId"] = correlationId;
        return ctx;
    }

    // ── 1. Null response → 500 with INTERNAL_ERROR ──

    [Fact]
    public void ToActionResult_NullResponse_Returns500WithInternalError()
    {
        ApiResponse<string>? response = null;
        var httpContext = CreateHttpContext("test-corr");

        var result = response.ToActionResult(httpContext);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);

        var body = Assert.IsType<ApiResponse<string>>(objectResult.Value);
        Assert.False(body.Success);
        Assert.Equal("INTERNAL_ERROR", body.ErrorCode);
        Assert.Equal(9999, body.ErrorValue);
        Assert.Equal("An unexpected null response was received.", body.Message);
        Assert.Equal("98", body.ResponseCode);
        Assert.Equal("test-corr", body.CorrelationId);
    }

    // ── 2. Exact-match ErrorCode regression anchors ──

    [Theory]
    [InlineData("INVALID_CREDENTIALS", 401)]
    [InlineData("TOKEN_REVOKED", 401)]
    [InlineData("REFRESH_TOKEN_REUSE", 401)]
    [InlineData("SESSION_EXPIRED", 401)]
    [InlineData("INSUFFICIENT_PERMISSIONS", 403)]
    [InlineData("DEPARTMENT_ACCESS_DENIED", 403)]
    [InlineData("ORGANIZATION_MISMATCH", 403)]
    [InlineData("ACCOUNT_LOCKED", 423)]
    [InlineData("RATE_LIMIT_EXCEEDED", 429)]
    [InlineData("INTERNAL_ERROR", 500)]
    [InlineData("SERVICE_UNAVAILABLE", 503)]
    public void ToActionResult_ExactMatchErrorCode_ReturnsExpectedStatusCode(string errorCode, int expectedStatus)
    {
        var response = new ApiResponse<string>
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorValue = 1000,
            Message = $"Error: {errorCode}",
            ResponseCode = "99",
            ResponseDescription = "error"
        };
        var httpContext = CreateHttpContext();

        var result = response.ToActionResult(httpContext);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
    }

    // ── 3. Pattern-match ErrorCodes ──

    [Theory]
    [InlineData("PLAN_NOT_FOUND", 404)]
    [InlineData("SUBSCRIPTION_ALREADY_EXISTS", 409)]
    [InlineData("ORGANIZATION_NAME_DUPLICATE", 409)]
    [InlineData("CONFLICT", 409)]
    [InlineData("PAYMENT_PROVIDER_ERROR", 502)]
    public void ToActionResult_PatternMatchErrorCode_ReturnsExpectedStatusCode(string errorCode, int expectedStatus)
    {
        var response = new ApiResponse<string>
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorValue = 2000,
            Message = $"Error: {errorCode}",
            ResponseCode = "99",
            ResponseDescription = "error"
        };
        var httpContext = CreateHttpContext();

        var result = response.ToActionResult(httpContext);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
    }

    // ── 4. Default fallback: Unknown error code → 400 ──

    [Fact]
    public void ToActionResult_UnknownErrorCode_Returns400()
    {
        var response = new ApiResponse<string>
        {
            Success = false,
            ErrorCode = "TOTALLY_UNKNOWN_ERROR",
            ErrorValue = 7777,
            Message = "Unknown error",
            ResponseCode = "99",
            ResponseDescription = "unknown"
        };
        var httpContext = CreateHttpContext();

        var result = response.ToActionResult(httpContext);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // ── 5. Null/empty ErrorCode → 400 ──

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToActionResult_NullOrEmptyErrorCode_Returns400(string? errorCode)
    {
        var response = new ApiResponse<string>
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorValue = 1000,
            Message = "Missing error code",
            ResponseCode = "99",
            ResponseDescription = "error"
        };
        var httpContext = CreateHttpContext();

        var result = response.ToActionResult(httpContext);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // ── 6. ToBadRequest with null HttpContext → CorrelationId is null ──

    [Fact]
    public void ToBadRequest_NullHttpContext_CorrelationIdIsNull()
    {
        var result = ApiResponseExtensions.ToBadRequest("Invalid input", httpContext: null);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);

        var body = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(body.Success);
        Assert.Equal("VALIDATION_ERROR", body.ErrorCode);
        Assert.Equal(1000, body.ErrorValue);
        Assert.Equal("Invalid input", body.Message);
        Assert.Equal("96", body.ResponseCode);
        Assert.Null(body.CorrelationId);
    }

    // ── 7. Success with custom status code 201 ──

    [Fact]
    public void ToActionResult_SuccessWithCustomStatusCode201_ReturnsObjectResultWith201()
    {
        var response = ApiResponse<string>.Ok("created-resource", "Resource created.");
        var httpContext = CreateHttpContext("corr-201");

        var result = response.ToActionResult(httpContext, 201);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);

        var body = Assert.IsType<ApiResponse<string>>(objectResult.Value);
        Assert.True(body.Success);
        Assert.Equal("created-resource", body.Data);
        Assert.Equal("corr-201", body.CorrelationId);
    }

    // ── 8. Success with no custom status code → 200 (OkObjectResult) ──

    [Fact]
    public void ToActionResult_SuccessNoCustomStatusCode_ReturnsOkObjectResult()
    {
        var response = ApiResponse<string>.Ok("some-data", "All good.");
        var httpContext = CreateHttpContext("corr-200");

        var result = response.ToActionResult(httpContext);

        var okResult = Assert.IsType<OkObjectResult>(result);

        var body = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(body.Success);
        Assert.Equal("some-data", body.Data);
        Assert.Equal("corr-200", body.CorrelationId);
    }
}
