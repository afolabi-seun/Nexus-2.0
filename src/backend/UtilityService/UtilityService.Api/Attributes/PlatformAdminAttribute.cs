using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UtilityService.Application.DTOs;

namespace UtilityService.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PlatformAdminAttribute : Attribute, IAsyncAuthorizationFilter
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

        var roleName = httpContext.Items.TryGetValue("roleName", out var rObj) ? rObj as string : null;
        if (roleName != "PlatformAdmin")
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
            ErrorCode = "PLATFORM_ADMIN_REQUIRED",
            Message = "PlatformAdmin access required.",
            CorrelationId = correlationId,
            ResponseCode = "03",
            ResponseDescription = "PlatformAdmin access required."
        };

        context.Result = new JsonResult(response) { StatusCode = 403 };
    }
}
