using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services.RateLimiter;

namespace SecurityService.Api.Middleware;

public class AuthenticatedRateLimiterMiddleware
{
    private const int DefaultMaxRequests = 100;
    private static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(1);
    private readonly RequestDelegate _next;

    public AuthenticatedRateLimiterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.Items.TryGetValue("userId", out var userIdObj)
            && userIdObj is string userId
            && !string.IsNullOrEmpty(userId))
        {
            var rateLimiter = context.RequestServices.GetRequiredService<IRateLimiterService>();
            var endpoint = context.Request.Path.Value ?? "/";

            var (isAllowed, retryAfter) = await rateLimiter.CheckRateLimitAsync(
                userId, $"auth:{endpoint}",
                DefaultMaxRequests, DefaultWindow,
                context.RequestAborted);

            if (!isAllowed)
                throw new RateLimitExceededException(retryAfter);
        }

        await _next(context);
    }
}
