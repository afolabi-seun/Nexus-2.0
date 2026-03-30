using ProfileService.Application.DTOs;

namespace ProfileService.Api.Middleware;

public class FirstTimeUserMiddleware
{
    private readonly RequestDelegate _next;

    public FirstTimeUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var isFirstTime = false;

            if (context.Items.TryGetValue("IsFirstTimeUser", out var ftObj) && ftObj is string ftStr)
            {
                isFirstTime = string.Equals(ftStr, "true", StringComparison.OrdinalIgnoreCase);
            }

            if (isFirstTime)
            {
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                var method = context.Request.Method;

                var isAllowed = method == HttpMethods.Post && path == "/api/v1/password/forced-change";

                if (!isAllowed)
                {
                    var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
                    var response = new ApiResponse<object>
                    {
                        Success = false,
                        ErrorCode = "FIRST_TIME_USER_RESTRICTED",
                        Message = "First-time users must change their password before accessing other endpoints.",
                        CorrelationId = correlationId
                    };

                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(response);
                    return;
                }
            }
        }

        await _next(context);
    }
}
