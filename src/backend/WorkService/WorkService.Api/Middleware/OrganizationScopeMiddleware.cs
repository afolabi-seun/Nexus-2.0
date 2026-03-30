using WorkService.Domain.Exceptions;
using WorkService.Domain.Helpers;

namespace WorkService.Api.Middleware;

public class OrganizationScopeMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationScopeMiddleware(RequestDelegate next)
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

        // Bypass for PlatformAdmin
        var roleName = context.Items.TryGetValue("roleName", out var rObj) ? rObj as string : null;
        if (roleName == RoleNames.PlatformAdmin)
        {
            await _next(context);
            return;
        }

        var orgId = context.Items.TryGetValue("organizationId", out var oObj) ? oObj as string : null;

        if (string.IsNullOrEmpty(orgId))
        {
            await _next(context);
            return;
        }

        // Check route parameter
        if (context.Request.RouteValues.TryGetValue("organizationId", out var routeOrg)
            && routeOrg?.ToString() is string routeOrgStr
            && !string.IsNullOrEmpty(routeOrgStr)
            && routeOrgStr != orgId)
        {
            throw new OrganizationMismatchException();
        }

        // Check query parameter
        if (context.Request.Query.TryGetValue("organizationId", out var queryOrg)
            && !string.IsNullOrEmpty(queryOrg)
            && queryOrg.ToString() != orgId)
        {
            throw new OrganizationMismatchException();
        }

        await _next(context);
    }
}
