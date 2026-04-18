using ProfileService.Domain.Exceptions;
using StackExchange.Redis;
using ProfileService.Infrastructure.Redis;

namespace ProfileService.Api.Middleware;

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

        // Rate limit unauthenticated invite endpoints
        var isRateLimited = (method == HttpMethods.Get && path.Contains("/invites/") && path.EndsWith("/validate"))
                         || (method == HttpMethods.Post && path.Contains("/invites/") && path.EndsWith("/accept"));

        if (!isRateLimited)
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

        if (count > 30)
        {
            var ttl = await db.KeyTimeToLiveAsync(key);
            var retryAfter = ttl.HasValue ? (int)ttl.Value.TotalSeconds : 60;
            throw new RateLimitExceededException(retryAfter);
        }

        await _next(context);
    }
}
