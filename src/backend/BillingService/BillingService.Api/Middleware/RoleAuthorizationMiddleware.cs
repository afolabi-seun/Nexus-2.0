using BillingService.Api.Attributes;
using BillingService.Application.DTOs;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Services.ErrorCodeResolver;

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
            await WriteErrorResponse(context, ErrorCodes.InsufficientPermissions,
                ErrorCodes.InsufficientPermissionsValue, "No role assigned.");
            return;
        }

        var endpoint = context.GetEndpoint();

        // Check if endpoint requires PlatformAdmin
        var requiresPlatformAdmin = endpoint?.Metadata.GetMetadata<PlatformAdminAttribute>() is not null;
        if (requiresPlatformAdmin)
        {
            if (roleName != "PlatformAdmin")
            {
                await WriteErrorResponse(context, "PLATFORM_ADMIN_REQUIRED",
                    ErrorCodes.InsufficientPermissionsValue, "PlatformAdmin access required.");
                return;
            }

            await _next(context);
            return;
        }

        // Check if endpoint requires OrgAdmin
        var requiresOrgAdmin = endpoint?.Metadata.GetMetadata<OrgAdminAttribute>() is not null;
        if (requiresOrgAdmin)
        {
            if (roleName != "OrgAdmin" && roleName != "PlatformAdmin")
            {
                await WriteErrorResponse(context, "ORGADMIN_REQUIRED",
                    ErrorCodes.InsufficientPermissionsValue, "OrgAdmin access required.");
                return;
            }
        }

        await _next(context);
    }

    private static async Task WriteErrorResponse(HttpContext context, string errorCode, int errorValue, string message)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var resolver = context.RequestServices?.GetService<IErrorCodeResolverService>();
        var (responseCode, responseDescription) = resolver is not null
            ? await resolver.ResolveAsync(errorCode, context.RequestAborted)
            : (errorCode, message);

        var response = new ApiResponse<object>
        {
            Success = false,
            ErrorValue = errorValue,
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = correlationId,
            ResponseCode = responseCode,
            ResponseDescription = responseDescription
        };

        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
    }
}
