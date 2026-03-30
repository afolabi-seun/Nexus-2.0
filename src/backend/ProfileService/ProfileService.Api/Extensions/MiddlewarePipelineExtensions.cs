using ProfileService.Api.Middleware;
using Serilog;

namespace ProfileService.Api.Extensions;

public static class MiddlewarePipelineExtensions
{
    public static WebApplication UseProfilePipeline(this WebApplication app)
    {
        // 1. CORS
        app.UseCors("NexusPolicy");

        // 2. Serilog request logging
        app.UseSerilogRequestLogging();

        // 3. CorrelationId
        app.UseMiddleware<CorrelationIdMiddleware>();

        // 3. GlobalExceptionHandler
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // 4. RateLimiter (unauthenticated endpoints)
        app.UseMiddleware<RateLimiterMiddleware>();

        // 5. Routing
        app.UseRouting();

        // 6. Authentication
        app.UseAuthentication();

        // 7. Authorization
        app.UseAuthorization();

        // 8. JwtClaims
        app.UseMiddleware<JwtClaimsMiddleware>();

        // 9. TokenBlacklist
        app.UseMiddleware<TokenBlacklistMiddleware>();

        // 10. FirstTimeUserGuard
        app.UseMiddleware<FirstTimeUserMiddleware>();

        // 11. RoleAuthorization
        app.UseMiddleware<RoleAuthorizationMiddleware>();

        // 12. OrganizationScope
        app.UseMiddleware<OrganizationScopeMiddleware>();

        return app;
    }
}
