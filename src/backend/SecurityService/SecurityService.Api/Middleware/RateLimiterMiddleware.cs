using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services.RateLimiter;
using SecurityService.Infrastructure.Configuration;

namespace SecurityService.Api.Middleware;

public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimiterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method;

        if (method != HttpMethods.Post)
        {
            await _next(context);
            return;
        }

        var rateLimiter = context.RequestServices.GetRequiredService<IRateLimiterService>();
        var appSettings = context.RequestServices.GetRequiredService<AppSettings>();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (path == "/api/v1/auth/login")
        {
            var (isAllowed, retryAfter) = await rateLimiter.CheckRateLimitAsync(
                ipAddress, "/api/v1/auth/login",
                appSettings.LoginRateLimitMax,
                TimeSpan.FromMinutes(appSettings.LoginRateLimitWindowMinutes),
                context.RequestAborted);

            if (!isAllowed)
                throw new RateLimitExceededException(retryAfter);
        }
        else if (path == "/api/v1/auth/otp/request")
        {
            var (isAllowed, retryAfter) = await rateLimiter.CheckRateLimitAsync(
                ipAddress, "/api/v1/auth/otp/request",
                appSettings.OtpRateLimitMax,
                TimeSpan.FromMinutes(appSettings.OtpRateLimitWindowMinutes),
                context.RequestAborted);

            if (!isAllowed)
                throw new RateLimitExceededException(retryAfter);
        }

        await _next(context);
    }
}
