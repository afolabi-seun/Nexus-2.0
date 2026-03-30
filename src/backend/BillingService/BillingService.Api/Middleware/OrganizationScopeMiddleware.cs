using BillingService.Application.DTOs;

namespace BillingService.Api.Middleware;

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
            await WriteErrorResponse(context);
            return;
        }

        // Check query parameter
        if (context.Request.Query.TryGetValue("organizationId", out var queryOrg)
            && !string.IsNullOrEmpty(queryOrg)
            && queryOrg.ToString() != orgId)
        {
            await WriteErrorResponse(context);
            return;
        }

        await _next(context);
    }

    private static async Task WriteErrorResponse(HttpContext context)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorCode = "ORGANIZATION_MISMATCH",
            Message = "Organization ID mismatch.",
            CorrelationId = correlationId
        };

        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }
}
