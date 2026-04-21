using Microsoft.AspNetCore.Mvc;
using UtilityService.Application.DTOs;
using UtilityService.Domain.Results;

namespace UtilityService.Api.Extensions;

/// <summary>
/// Converts ServiceResult to IActionResult wrapped in ApiResponse envelope.
/// </summary>
public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result)
    {
        var httpContext = new HttpContextAccessor().HttpContext;
        var correlationId = httpContext?.Items["CorrelationId"]?.ToString();

        if (result is null)
        {
            var error = ApiResponse<object>.Fail(9999, "INTERNAL_ERROR", "An unexpected error occurred.");
            error.CorrelationId = correlationId;
            return new ObjectResult(error) { StatusCode = 500 };
        }

        if (!result.IsSuccess)
        {
            var error = ApiResponse<object>.Fail(
                result.ErrorValue ?? 0,
                result.ErrorCode ?? "INTERNAL_ERROR",
                result.Message ?? "An error occurred.");
            error.CorrelationId = correlationId;
            return new ObjectResult(error) { StatusCode = result.StatusCode };
        }

        var response = ApiResponse<T>.Ok(result.Data!, result.Message);
        response.CorrelationId = correlationId;
        return new ObjectResult(response) { StatusCode = result.StatusCode };
    }
}
