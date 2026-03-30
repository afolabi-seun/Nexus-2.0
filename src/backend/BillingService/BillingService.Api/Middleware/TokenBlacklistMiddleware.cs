using BillingService.Application.DTOs;
using StackExchange.Redis;

namespace BillingService.Api.Middleware;

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
            {
                var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
                var response = new ApiResponse<object>
                {
                    Success = false,
                    ErrorCode = "TOKEN_REVOKED",
                    Message = "Token has been revoked.",
                    CorrelationId = correlationId
                };

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(response);
                return;
            }
        }

        await _next(context);
    }
}
