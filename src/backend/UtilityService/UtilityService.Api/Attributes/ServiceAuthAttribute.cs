using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UtilityService.Application.DTOs;

namespace UtilityService.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ServiceAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            SetForbiddenResult(context);
            return;
        }

        var serviceIdClaim = user.FindFirst("serviceId");
        if (serviceIdClaim is null || string.IsNullOrEmpty(serviceIdClaim.Value))
        {
            SetForbiddenResult(context);
            return;
        }

        await Task.CompletedTask;
    }

    private static void SetForbiddenResult(AuthorizationFilterContext context)
    {
        var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorCode = "SERVICE_NOT_AUTHORIZED",
            Message = "Service is not authorized to perform this action.",
            CorrelationId = correlationId,
            ResponseCode = "03",
            ResponseDescription = "Service is not authorized to perform this action."
        };

        context.Result = new JsonResult(response) { StatusCode = 403 };
    }
}
