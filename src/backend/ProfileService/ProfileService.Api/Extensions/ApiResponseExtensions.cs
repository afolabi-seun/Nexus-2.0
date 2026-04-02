using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Application.DTOs;

namespace ProfileService.Api.Extensions;

public static class ApiResponseExtensions
{
    public static IActionResult ToActionResult<T>(
        this ApiResponse<T>? response,
        HttpContext httpContext,
        int? successStatusCode = null)
    {
        // 1. Null guard
        if (response is null)
        {
            var errorResponse = new ApiResponse<T>
            {
                Success = false,
                ErrorCode = "INTERNAL_ERROR",
                ErrorValue = 9999,
                Message = "An unexpected null response was received.",
                ResponseCode = "98",
                ResponseDescription = "An unexpected null response was received.",
                CorrelationId = httpContext.Items["CorrelationId"]?.ToString()
            };
            return new ObjectResult(errorResponse) { StatusCode = 500 };
        }

        // 2. Inject CorrelationId
        var correlationId = httpContext.Items["CorrelationId"]?.ToString();
        if (correlationId is not null)
        {
            response.CorrelationId = correlationId;
        }

        // 3. Error path — ignore custom status code
        if (!response.Success)
        {
            var statusCode = DetermineStatusCodeFromErrorCode(response.ErrorCode);
            return new ObjectResult(response) { StatusCode = statusCode };
        }

        // 4. Success path — use custom status code or default 200
        if (successStatusCode.HasValue)
        {
            return new ObjectResult(response) { StatusCode = successStatusCode.Value };
        }

        return new OkObjectResult(response);
    }

    public static IActionResult ToBadRequest(string message, HttpContext? httpContext = null)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorCode = "VALIDATION_ERROR",
            ErrorValue = 1000,
            Message = message,
            ResponseCode = "96",
            ResponseDescription = message,
            CorrelationId = httpContext?.Items["CorrelationId"]?.ToString()
        };
        return new ObjectResult(response) { StatusCode = 400 };
    }

    internal static int DetermineStatusCodeFromErrorCode(string? errorCode)
    {
        if (string.IsNullOrEmpty(errorCode))
            return 400;

        return errorCode switch
        {
            "INVALID_CREDENTIALS" or "TOKEN_REVOKED" or "REFRESH_TOKEN_REUSE"
                or "SESSION_EXPIRED" => 401,
            "INSUFFICIENT_PERMISSIONS" or "DEPARTMENT_ACCESS_DENIED"
                or "ORGANIZATION_MISMATCH" => 403,
            "ACCOUNT_LOCKED" => 423,
            "RATE_LIMIT_EXCEEDED" => 429,
            "INTERNAL_ERROR" => 500,
            "SERVICE_UNAVAILABLE" => 503,
            _ when errorCode.Contains("NOT_FOUND") => 404,
            _ when errorCode.Contains("ALREADY_EXISTS")
                || errorCode.Contains("DUPLICATE")
                || errorCode.Contains("CONFLICT") => 409,
            _ when errorCode.Contains("PAYMENT_PROVIDER_ERROR") => 502,
            _ => 400
        };
    }
}
