using System.Text.Json;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Infrastructure.Redis;

namespace SecurityService.Api.Middleware;

public class ErrorResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorResponseLoggingMiddleware> _logger;
    private const string ServiceName = "SecurityService";

    public ErrorResponseLoggingMiddleware(RequestDelegate next, ILogger<ErrorResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOutboxService outboxService)
    {
        await _next(context);

        if (context.Response.StatusCode >= 500 &&
            !context.Items.ContainsKey("ErrorLogged"))
        {
            try
            {
                var correlationId = context.Items["CorrelationId"]?.ToString();
                var tenantId = context.Items["TenantId"]?.ToString();

                var envelope = new
                {
                    Type = "error",
                    Payload = new
                    {
                        TenantId = tenantId,
                        ServiceName,
                        ErrorCode = $"HTTP_{context.Response.StatusCode}",
                        Message = $"{context.Request.Method} {context.Request.Path} returned {context.Response.StatusCode}",
                        CorrelationId = correlationId,
                        Severity = "Error"
                    },
                    Timestamp = DateTime.UtcNow
                };

                await outboxService.PublishAsync(RedisKeys.Outbox, JsonSerializer.Serialize(envelope));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish error log for {StatusCode} response.", context.Response.StatusCode);
            }
        }
    }
}
