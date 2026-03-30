using SecurityService.Domain.Exceptions;
using StackExchange.Redis;

namespace SecurityService.Api.Middleware;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.Items.TryGetValue("jti", out var jtiObj)
            && jtiObj is string jti
            && !string.IsNullOrEmpty(jti))
        {
            var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
            var db = redis.GetDatabase();
            var isBlacklisted = await db.KeyExistsAsync($"blacklist:{jti}");

            if (isBlacklisted)
                throw new TokenRevokedException();
        }

        await _next(context);
    }
}
