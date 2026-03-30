using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SecurityService.Application.DTOs;
using SecurityService.Domain.Exceptions;

namespace SecurityService.Api.Attributes;

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
        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorValue = ErrorCodes.ServiceNotAuthorizedValue,
            ErrorCode = ErrorCodes.ServiceNotAuthorized,
            Message = "Service is not authorized to perform this action.",
            ResponseCode = "03",
            ResponseDescription = "Service is not authorized to perform this action."
        };

        context.Result = new JsonResult(response) { StatusCode = 403 };
    }
}
