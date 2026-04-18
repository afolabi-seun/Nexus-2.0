using System.Text.Json;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Infrastructure.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.Outbox;

public class OutboxService : IOutboxService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<OutboxService> _logger;

    private const int MaxRetries = 3;
    private static readonly int[] BackoffSecondsPerRetry = [1, 2, 4];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxService(IConnectionMultiplexer redis, ILogger<OutboxService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishAsync(object message, CancellationToken ct = default)
    {
        var serialized = JsonSerializer.Serialize(message, JsonOptions);
        var db = _redis.GetDatabase();

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                await db.ListLeftPushAsync(RedisKeys.Outbox, serialized);
                _logger.LogInformation("Message published to {QueueKey}", RedisKeys.Outbox);
                return;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish to {QueueKey} (attempt {Attempt}/{MaxRetries})",
                    RedisKeys.Outbox, attempt + 1, MaxRetries);

                if (attempt < MaxRetries - 1)
                    await Task.Delay(TimeSpan.FromSeconds(BackoffSecondsPerRetry[attempt]), ct);
            }
        }

        try
        {
            await db.ListLeftPushAsync(RedisKeys.Dlq, serialized);
            _logger.LogError(
                "Message moved to dead-letter queue {DlqKey} after {MaxRetries} failed attempts",
                RedisKeys.Dlq, MaxRetries);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex,
                "Failed to publish to dead-letter queue {DlqKey}. Message lost.", RedisKeys.Dlq);
        }
    }
}
