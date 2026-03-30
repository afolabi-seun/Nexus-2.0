using SecurityService.Api.Attributes;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Helpers;

namespace SecurityService.Api.Middleware;

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
            throw new InsufficientPermissionsException("No role assigned.");
        }

        // Check if endpoint requires PlatformAdmin
        var endpoint = context.GetEndpoint();
        var requiresPlatformAdmin = endpoint?.Metadata.GetMetadata<PlatformAdminAttribute>() is not null;

        if (requiresPlatformAdmin && roleName != RoleNames.PlatformAdmin)
        {
            throw new InsufficientPermissionsException("PlatformAdmin role required.");
        }

        // PlatformAdmin has full access (platform-wide)
        if (roleName == RoleNames.PlatformAdmin)
        {
            await _next(context);
            return;
        }

        // OrgAdmin has full access
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
                throw new DepartmentAccessDeniedException();
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
                throw new DepartmentAccessDeniedException();
            }

            await _next(context);
            return;
        }

        throw new InsufficientPermissionsException($"Unknown role: {roleName}");
    }
}
