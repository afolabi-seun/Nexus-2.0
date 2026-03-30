using BillingService.Api.Attributes;
using BillingService.Application.DTOs;

namespace BillingService.Api.Middleware;

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

        if (string.IsNullOrEmpty(roleName))
        {
            await WriteErrorResponse(context, "No role assigned.");
            return;
        }

        // Check if endpoint requires OrgAdmin
        var endpoint = context.GetEndpoint();
        var requiresOrgAdmin = endpoint?.Metadata.GetMetadata<OrgAdminAttribute>() is not null;

        if (requiresOrgAdmin && roleName != "OrgAdmin")
        {
            await WriteErrorResponse(context, "OrgAdmin access required.");
            return;
        }

        await _next(context);
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
