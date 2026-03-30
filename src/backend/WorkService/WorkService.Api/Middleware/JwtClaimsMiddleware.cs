using System.Security.Claims;

namespace WorkService.Api.Middleware;

public class JwtClaimsMiddleware
{
    private readonly RequestDelegate _next;

    public JwtClaimsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claims = context.User.Claims.ToList();

            SetItem(context, claims, "userId", ClaimTypes.NameIdentifier);
            SetItem(context, claims, "organizationId", "organizationId");
            SetItem(context, claims, "departmentId", "departmentId");
            SetItem(context, claims, "roleName", "roleName");
            SetItem(context, claims, "departmentRole", "departmentRole");
            SetItem(context, claims, "deviceId", "deviceId");
            SetItem(context, claims, "jti", "jti");
            SetItem(context, claims, "serviceId", "serviceId");

            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim is not null && long.TryParse(expClaim.Value, out var expUnix))
            {
                context.Items["tokenExpiry"] = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            }
        }

        await _next(context);
    }

    private static void SetItem(HttpContext context, List<Claim> claims, string itemKey, string claimType)
    {
        var claim = claims.FirstOrDefault(c => c.Type == claimType);
        if (claim is not null)
        {
            context.Items[itemKey] = claim.Value;
        }
    }
}
