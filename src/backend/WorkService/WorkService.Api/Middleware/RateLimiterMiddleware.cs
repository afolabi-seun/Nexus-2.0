using WorkService.Domain.Exceptions;
using StackExchange.Redis;

namespace WorkService.Api.Middleware;

public class RateLimiterMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimiterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // WorkService has no unauthenticated endpoints that need rate limiting
        // All endpoints require JWT Bearer auth
        await _next(context);
    }
}
