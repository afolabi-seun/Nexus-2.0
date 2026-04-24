using SecurityService.Api.Middleware;
using Serilog;

namespace SecurityService.Api.Extensions;

public static class MiddlewarePipelineExtensions
{
    public static WebApplication UseSecurityPipeline(this WebApplication app)
    {
        // 1. CORS
        app.UseCors("NexusPolicy");

        // 2. Serilog request logging
        app.UseSerilogRequestLogging();

        // 3. CorrelationId
        app.UseMiddleware<CorrelationIdMiddleware>();

        // 4. GlobalExceptionHandler
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // 5. ErrorResponseLogging
        app.UseMiddleware<ErrorResponseLoggingMiddleware>();

        // 6. RateLimiter (unauthenticated endpoints)
        app.UseMiddleware<RateLimiterMiddleware>();

        // 7. Routing
        app.UseRouting();

        // 8. Authentication
        app.UseAuthentication();

        // 9. Authorization
        app.UseAuthorization();

        // 10. JwtClaims
        app.UseMiddleware<JwtClaimsMiddleware>();

        // 11. TokenBlacklist
        app.UseMiddleware<TokenBlacklistMiddleware>();

        // 12. FirstTimeUserGuard
        app.UseMiddleware<FirstTimeUserMiddleware>();

        // 13. AuthenticatedRateLimiter
        app.UseMiddleware<AuthenticatedRateLimiterMiddleware>();

        // 14. RoleAuthorization
        app.UseMiddleware<RoleAuthorizationMiddleware>();

        // 15. OrganizationScope
        app.UseMiddleware<OrganizationScopeMiddleware>();

        return app;
    }
}
