using WorkService.Api.Middleware;
using Serilog;

namespace WorkService.Api.Extensions;

public static class MiddlewarePipelineExtensions
{
    public static WebApplication UseWorkServicePipeline(this WebApplication app)
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

        // 6. RateLimiter
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

        // Note: No FirstTimeUserMiddleware — enforced by SecurityService

        // 12. RoleAuthorization
        app.UseMiddleware<RoleAuthorizationMiddleware>();

        // 13. OrganizationScope
        app.UseMiddleware<OrganizationScopeMiddleware>();

        return app;
    }
}
