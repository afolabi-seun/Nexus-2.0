using UtilityService.Api.Middleware;
using Serilog;

namespace UtilityService.Api.Extensions;

public static class MiddlewarePipelineExtensions
{
    public static WebApplication UseUtilityPipeline(this WebApplication app)
    {
        // 1. CORS
        app.UseCors("NexusPolicy");

        // 2. Serilog request logging
        app.UseSerilogRequestLogging();

        // 3. CorrelationId
        app.UseMiddleware<CorrelationIdMiddleware>();

        // 3. GlobalExceptionHandler
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // 4. Routing
        app.UseRouting();

        // 5. Authentication
        app.UseAuthentication();

        // 6. Authorization
        app.UseAuthorization();

        // 7. JwtClaims
        app.UseMiddleware<JwtClaimsMiddleware>();

        // 8. TokenBlacklist
        app.UseMiddleware<TokenBlacklistMiddleware>();

        // 9. OrganizationScope
        app.UseMiddleware<OrganizationScopeMiddleware>();

        // Note: NO FirstTimeUserMiddleware, NO RateLimiterMiddleware

        return app;
    }
}
