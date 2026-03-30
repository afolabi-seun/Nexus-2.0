using ProfileService.Api.Attributes;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Helpers;

namespace ProfileService.Api.Middleware;

public class RoleAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public RoleAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // Skip for service-auth tokens
        if (context.Items.ContainsKey("serviceId"))
        {
            await _next(context);
            return;
        }

        var roleName = context.Items.TryGetValue("roleName", out var rObj) ? rObj as string : null;
        var departmentId = context.Items.TryGetValue("departmentId", out var dObj) ? dObj as string : null;

        if (string.IsNullOrEmpty(roleName))
        {
            await WriteErrorResponse(context, "No role assigned.");
            return;
        }

        // Check if endpoint requires PlatformAdmin
        var endpoint = context.GetEndpoint();
        var requiresPlatformAdmin = endpoint?.Metadata.GetMetadata<PlatformAdminAttribute>() is not null;

        if (requiresPlatformAdmin)
        {
            if (roleName != RoleNames.PlatformAdmin)
            {
                await WriteErrorResponse(context, "PlatformAdmin access required.");
                return;
            }

            await _next(context);
            return;
        }

        // PlatformAdmin has full access to non-PlatformAdmin-decorated endpoints too
        if (roleName == RoleNames.PlatformAdmin)
        {
            await _next(context);
            return;
        }

        // OrgAdmin has full organization-wide access
        if (roleName == RoleNames.OrgAdmin)
        {
            await _next(context);
            return;
        }

        // DeptLead — check department scope if route has departmentId
        if (roleName == RoleNames.DeptLead)
        {
            var routeDeptId = context.Request.RouteValues.TryGetValue("departmentId", out var rd) ? rd?.ToString() : null;
            var queryDeptId = context.Request.Query.ContainsKey("departmentId") ? context.Request.Query["departmentId"].ToString() : null;
            var targetDeptId = routeDeptId ?? queryDeptId;

            if (!string.IsNullOrEmpty(targetDeptId) && targetDeptId != departmentId)
            {
                await WriteErrorResponse(context, "Department access denied.");
                return;
            }

            await _next(context);
            return;
        }

        // Member / Viewer — enforce department match if target department is specified
        if (roleName == RoleNames.Member || roleName == RoleNames.Viewer)
        {
            var routeDeptId = context.Request.RouteValues.TryGetValue("departmentId", out var rd) ? rd?.ToString() : null;
            var queryDeptId = context.Request.Query.ContainsKey("departmentId") ? context.Request.Query["departmentId"].ToString() : null;
            var targetDeptId = routeDeptId ?? queryDeptId;

            if (!string.IsNullOrEmpty(targetDeptId) && targetDeptId != departmentId)
            {
                await WriteErrorResponse(context, "Department access denied.");
                return;
            }

            await _next(context);
            return;
        }

        await WriteErrorResponse(context, $"Unknown role: {roleName}");
    }

    private static async Task WriteErrorResponse(HttpContext context, string message)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorCode = "INSUFFICIENT_PERMISSIONS",
            Message = message,
            CorrelationId = correlationId
        };

        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }
}
