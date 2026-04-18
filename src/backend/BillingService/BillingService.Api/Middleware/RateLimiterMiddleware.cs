using StackExchange.Redis;
using BillingService.Infrastructure.Redis;

namespace BillingService.Api.Middleware;

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

        // IP-based rate limiting for webhook endpoint
        var isWebhook = method == HttpMethods.Post && path.Contains("/webhooks/stripe");

        if (!isWebhook)
        {
            await _next(context);
            return;
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();

        var key = RedisKeys.RateLimit(ipAddress, path);
        var count = await db.StringIncrementAsync(key);

        if (count == 1)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(1));
        }

        if (count > 60)
        {
            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            return;
        }

        await _next(context);
    }
}
