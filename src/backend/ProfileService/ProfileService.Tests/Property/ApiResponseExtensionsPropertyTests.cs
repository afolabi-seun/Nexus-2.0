using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;

namespace ProfileService.Tests.Property;

/// <summary>
/// Property-based tests for ApiResponseExtensions.
/// Uses FsCheck.Xunit to verify correctness properties defined in the design document.
/// </summary>
public class ApiResponseExtensionsPropertyTests
{
    // ── Helpers ──

    private static DefaultHttpContext CreateHttpContext(string? correlationId = null)
    {
        var ctx = new DefaultHttpContext();
        if (correlationId is not null)
            ctx.Items["CorrelationId"] = correlationId;
        return ctx;
    }

    private static ApiResponse<string> GenerateRandomResponse(Random rng, bool? forceSuccess = null)
    {
        var success = forceSuccess ?? (rng.Next(2) == 0);
        var hasData = rng.Next(2) == 0;
        var hasErrorCode = rng.Next(2) == 0;
        var hasErrors = rng.Next(2) == 0;

        return new ApiResponse<string>
        {
            Success = success,
            ResponseCode = rng.Next(100).ToString("D2"),
            ResponseDescription = $"desc-{rng.Next(10000)}",
            Data = hasData ? $"data-{rng.Next(10000)}" : null,
            ErrorCode = hasErrorCode ? $"ERR_{rng.Next(10000)}" : null,
            ErrorValue = rng.Next(2) == 0 ? rng.Next(1, 10000) : null,
            Message = rng.Next(2) == 0 ? $"msg-{rng.Next(10000)}" : null,
            CorrelationId = null,
            Errors = hasErrors
                ? Enumerable.Range(0, rng.Next(1, 4))
                    .Select(_ => new ErrorDetail
                    {
                        Field = $"field-{rng.Next(100)}",
                        Message = $"error-{rng.Next(100)}"
                    }).ToList()
                : null
        };
    }

    // ── Reference mapping for Property 2 ──

    private static readonly Dictionary<string, int> ExactErrorCodeMap = new()
    {
        ["INVALID_CREDENTIALS"] = 401,
        ["TOKEN_REVOKED"] = 401,
        ["REFRESH_TOKEN_REUSE"] = 401,
        ["SESSION_EXPIRED"] = 401,
        ["INSUFFICIENT_PERMISSIONS"] = 403,
        ["DEPARTMENT_ACCESS_DENIED"] = 403,
        ["ORGANIZATION_MISMATCH"] = 403,
        ["ACCOUNT_LOCKED"] = 423,
        ["RATE_LIMIT_EXCEEDED"] = 429,
        ["INTERNAL_ERROR"] = 500,
        ["SERVICE_UNAVAILABLE"] = 503,
    };

    private static readonly (string code, int status)[] PatternErrorCodes =
    [
        ("USER_NOT_FOUND", 404),
        ("ITEM_NOT_FOUND", 404),
        ("RESOURCE_NOT_FOUND", 404),
        ("ALREADY_EXISTS", 409),
        ("EMAIL_DUPLICATE", 409),
        ("NAME_DUPLICATE", 409),
        ("SCHEDULE_CONFLICT", 409),
        ("PAYMENT_PROVIDER_ERROR", 502),
    ];

    private static int ExpectedStatusCode(string? errorCode)
    {
        if (string.IsNullOrEmpty(errorCode))
            return 400;

        if (ExactErrorCodeMap.TryGetValue(errorCode, out var exact))
            return exact;

        if (errorCode.Contains("NOT_FOUND")) return 404;
        if (errorCode.Contains("ALREADY_EXISTS") || errorCode.Contains("DUPLICATE") || errorCode.Contains("CONFLICT")) return 409;
        if (errorCode.Contains("PAYMENT_PROVIDER_ERROR")) return 502;

        return 400;
    }

    // ── Property 1: Response body preserves all ApiResponse properties ──
    // Feature: standardized-api-responses, Property 1: Response body preserves all ApiResponse properties
    /// <summary>
    /// For any ApiResponse&lt;string&gt; instance, calling ToActionResult(HttpContext) produces an IActionResult
    /// whose body object contains the same ResponseCode, ResponseDescription, Success, Data, ErrorCode,
    /// ErrorValue, Message, and Errors values as the original ApiResponse.
    /// **Validates: Requirements 1.3, 9.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ResponseBody_PreservesAllApiResponseProperties(ushort seed)
    {
        var rng = new Random(seed);
        var response = GenerateRandomResponse(rng);
        var httpContext = CreateHttpContext();

        // Capture original values before ToActionResult mutates CorrelationId
        var origResponseCode = response.ResponseCode;
        var origResponseDescription = response.ResponseDescription;
        var origSuccess = response.Success;
        var origData = response.Data;
        var origErrorCode = response.ErrorCode;
        var origErrorValue = response.ErrorValue;
        var origMessage = response.Message;
        var origErrors = response.Errors;

        var result = response.ToActionResult(httpContext);

        var objectResult = result as ObjectResult;
        if (objectResult is null) return false;

        var body = objectResult.Value as ApiResponse<string>;
        if (body is null) return false;

        return body.ResponseCode == origResponseCode
            && body.ResponseDescription == origResponseDescription
            && body.Success == origSuccess
            && body.Data == origData
            && body.ErrorCode == origErrorCode
            && body.ErrorValue == origErrorValue
            && body.Message == origMessage
            && ReferenceEquals(body.Errors, origErrors);
    }

    // ── Property 2: ErrorCode-to-HTTP-status mapping is correct ──
    // Feature: standardized-api-responses, Property 2: ErrorCode-to-HTTP-status mapping is correct
    /// <summary>
    /// For known error codes from a reference lookup table, DetermineStatusCodeFromErrorCode returns
    /// the correct HTTP status code.
    /// **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10, 2.11, 10.2, 10.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ErrorCodeToHttpStatus_MappingIsCorrect(ushort seed)
    {
        var rng = new Random(seed);

        // Build a pool of all known error codes + some unknowns
        var allCodes = new List<string?>();
        allCodes.AddRange(ExactErrorCodeMap.Keys);
        allCodes.AddRange(PatternErrorCodes.Select(p => p.code));
        allCodes.Add("VALIDATION_ERROR");
        allCodes.Add(null);
        allCodes.Add("");
        allCodes.Add($"UNKNOWN_CODE_{rng.Next(10000)}");

        // Pick a random code from the pool
        var errorCode = allCodes[rng.Next(allCodes.Count)];

        // Test indirectly through ToActionResult since DetermineStatusCodeFromErrorCode is internal
        var response = new ApiResponse<string>
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorValue = 1000,
            Message = "test",
            ResponseCode = "99",
            ResponseDescription = "test"
        };
        var httpContext = CreateHttpContext();
        var result = response.ToActionResult(httpContext) as ObjectResult;

        if (result is null) return false;

        var expected = ExpectedStatusCode(errorCode);
        return result.StatusCode == expected;
    }

    // ── Property 3: Success responses use custom or default status code ──
    // Feature: standardized-api-responses, Property 3: Success responses use custom or default status code
    /// <summary>
    /// For any ApiResponse&lt;string&gt; with Success=true, calling ToActionResult with an optional
    /// successStatusCode returns the custom code when provided, or 200 when not.
    /// **Validates: Requirements 1.1, 1.2, 4.1, 4.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SuccessResponses_UseCustomOrDefaultStatusCode(ushort seed)
    {
        var rng = new Random(seed);
        var response = GenerateRandomResponse(rng, forceSuccess: true);
        var httpContext = CreateHttpContext();

        var useCustom = rng.Next(2) == 0;
        var customCode = rng.Next(200, 300); // e.g. 201, 202, etc.

        IActionResult result;
        int expectedStatus;

        if (useCustom)
        {
            result = response.ToActionResult(httpContext, customCode);
            expectedStatus = customCode;
        }
        else
        {
            result = response.ToActionResult(httpContext);
            expectedStatus = 200;
        }

        var objectResult = result as ObjectResult;
        if (objectResult is null) return false;

        // OkObjectResult has StatusCode null by default but represents 200
        var actualStatus = objectResult.StatusCode ?? 200;
        return actualStatus == expectedStatus;
    }

    // ── Property 4: Custom status code is ignored for error responses ──
    // Feature: standardized-api-responses, Property 4: Custom status code is ignored for error responses
    /// <summary>
    /// For any ApiResponse&lt;string&gt; with Success=false, any random ErrorCode, and any random custom
    /// status code, the result status code equals DetermineStatusCodeFromErrorCode(ErrorCode),
    /// ignoring the custom status code.
    /// **Validates: Requirements 4.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CustomStatusCode_IgnoredForErrorResponses(ushort seed)
    {
        var rng = new Random(seed);

        // Pick a random error code from known set
        var allCodes = new List<string>();
        allCodes.AddRange(ExactErrorCodeMap.Keys);
        allCodes.AddRange(PatternErrorCodes.Select(p => p.code));
        allCodes.Add("VALIDATION_ERROR");
        allCodes.Add($"RANDOM_ERROR_{rng.Next(10000)}");

        var errorCode = allCodes[rng.Next(allCodes.Count)];
        var customStatusCode = rng.Next(200, 600);

        var response = new ApiResponse<string>
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorValue = rng.Next(1000, 9999),
            Message = $"error-{rng.Next(10000)}",
            ResponseCode = "99",
            ResponseDescription = "error"
        };

        var httpContext = CreateHttpContext();
        var result = response.ToActionResult(httpContext, customStatusCode) as ObjectResult;

        if (result is null) return false;

        var expected = ExpectedStatusCode(errorCode);
        return result.StatusCode == expected;
    }

    // ── Property 5: CorrelationId injection from HttpContext ──
    // Feature: standardized-api-responses, Property 5: CorrelationId injection from HttpContext
    /// <summary>
    /// For any ApiResponse&lt;string&gt; and any CorrelationId string set in HttpContext.Items,
    /// the returned response body's CorrelationId matches. When null/absent, the original
    /// CorrelationId remains unchanged.
    /// **Validates: Requirements 5.2, 5.3, 6.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool CorrelationId_InjectedFromHttpContext(ushort seed)
    {
        var rng = new Random(seed);
        var response = GenerateRandomResponse(rng, forceSuccess: true);

        var hasCorrelationId = rng.Next(2) == 0;
        var correlationId = hasCorrelationId ? $"corr-{Guid.NewGuid()}" : null;
        var originalCorrelationId = response.CorrelationId; // should be null from generator

        var httpContext = CreateHttpContext(correlationId);
        var result = response.ToActionResult(httpContext) as ObjectResult;

        if (result is null) return false;

        var body = result.Value as ApiResponse<string>;
        if (body is null) return false;

        if (hasCorrelationId)
        {
            return body.CorrelationId == correlationId;
        }
        else
        {
            // When CorrelationId is not in HttpContext, original value is preserved
            return body.CorrelationId == originalCorrelationId;
        }
    }

    // ── Property 6: ToBadRequest produces correct structure ──
    // Feature: standardized-api-responses, Property 6: ToBadRequest produces correct structure
    /// <summary>
    /// For any non-null message string, ToBadRequest(message, httpContext) returns an IActionResult
    /// with HTTP 400, Success=false, ResponseCode="96", ErrorCode="VALIDATION_ERROR",
    /// ErrorValue=1000, and Message equal to the input.
    /// **Validates: Requirements 6.1, 6.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ToBadRequest_ProducesCorrectStructure(ushort seed)
    {
        var rng = new Random(seed);
        var message = $"Validation failed: field-{rng.Next(10000)} is invalid (code {rng.Next(100)})";
        var correlationId = $"corr-{Guid.NewGuid()}";
        var httpContext = CreateHttpContext(correlationId);

        var result = ApiResponseExtensions.ToBadRequest(message, httpContext) as ObjectResult;

        if (result is null) return false;
        if (result.StatusCode != 400) return false;

        var body = result.Value as ApiResponse<object>;
        if (body is null) return false;

        return !body.Success
            && body.ResponseCode == "96"
            && body.ErrorCode == "VALIDATION_ERROR"
            && body.ErrorValue == 1000
            && body.Message == message
            && body.CorrelationId == correlationId;
    }
}
