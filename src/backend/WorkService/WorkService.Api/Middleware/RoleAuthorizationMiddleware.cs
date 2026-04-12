using WorkService.Api.Attributes;
using WorkService.Application.DTOs;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Helpers;
using WorkService.Domain.Interfaces.Services.ErrorCodeResolver;

namespace WorkService.Api.Middleware;

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

        // Check OrgAdmin-only attribute
        var requiresOrgAdmin = endpoint?.Metadata.GetMetadata<OrgAdminAttribute>() is not null;
        if (requiresOrgAdmin)
        {
            if (roleName != RoleNames.OrgAdmin && roleName != RoleNames.PlatformAdmin)
            {
                await WriteErrorResponse(context, ErrorCodes.OrgAdminRequired,
                    ErrorCodes.OrgAdminRequiredValue, "OrgAdmin access required.");
                return;
            }

            await _next(context);
            return;
        }

        // Check DeptLead-only attribute
        var requiresDeptLead = endpoint?.Metadata.GetMetadata<DeptLeadAttribute>() is not null;
        if (requiresDeptLead)
        {
            if (roleName != RoleNames.OrgAdmin && roleName != RoleNames.DeptLead && roleName != RoleNames.PlatformAdmin)
            {
                await WriteErrorResponse(context, ErrorCodes.DeptLeadRequired,
                    ErrorCodes.DeptLeadRequiredValue, "DeptLead or higher access required.");
                return;
            }
        }

        // Check ServiceAuth attribute
        var requiresServiceAuth = endpoint?.Metadata.GetMetadata<ServiceAuthAttribute>() is not null;
        if (requiresServiceAuth)
        {
            if (!context.Items.ContainsKey("serviceId"))
            {
                await WriteErrorResponse(context, ErrorCodes.InsufficientPermissions,
                    ErrorCodes.InsufficientPermissionsValue, "Service authentication required.");
                return;
            }

            await _next(context);
            return;
        }

        // PlatformAdmin and OrgAdmin have full access
        if (roleName == RoleNames.PlatformAdmin || roleName == RoleNames.OrgAdmin)
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
            var userDeptId = context.Items.TryGetValue("departmentId", out var dObj) ? dObj as string : null;

            if (!string.IsNullOrEmpty(targetDeptId) && targetDeptId != userDeptId)
            {
                await WriteErrorResponse(context, ErrorCodes.DepartmentAccessDenied,
                    ErrorCodes.DepartmentAccessDeniedValue, "Department access denied.");
                return;
            }

            await _next(context);
            return;
        }

        // Member / Viewer
        if (roleName == RoleNames.Member || roleName == RoleNames.Viewer)
        {
            await _next(context);
            return;
        }

        await WriteErrorResponse(context, ErrorCodes.InsufficientPermissions,
            ErrorCodes.InsufficientPermissionsValue, $"Unknown role: {roleName}");
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
