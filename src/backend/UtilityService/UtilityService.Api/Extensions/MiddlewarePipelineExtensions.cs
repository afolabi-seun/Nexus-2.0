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

        // 4. GlobalExceptionHandler
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // 5. ErrorResponseLogging
        app.UseMiddleware<ErrorResponseLoggingMiddleware>();

        // 6. Routing
        app.UseRouting();

        // 7. Authentication
        app.UseAuthentication();

        // 8. Authorization
        app.UseAuthorization();

        // 9. JwtClaims
        app.UseMiddleware<JwtClaimsMiddleware>();

        // 10. TokenBlacklist
        app.UseMiddleware<TokenBlacklistMiddleware>();

        // 11. OrganizationScope
        app.UseMiddleware<OrganizationScopeMiddleware>();

        // Note: NO FirstTimeUserMiddleware, NO RateLimiterMiddleware

        return app;
    }
}
