using System.Text.Json;
using BillingService.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.Outbox;

public class OutboxService : IOutboxService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(IConnectionMultiplexer redis, ILogger<OutboxService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishAsync(object message, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(message);
            await db.ListLeftPushAsync("outbox:billing", json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to outbox:billing");
        }
    }
}
