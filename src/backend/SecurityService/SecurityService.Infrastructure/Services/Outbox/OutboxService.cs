using Microsoft.Extensions.Logging;
using SecurityService.Domain.Interfaces.Services.Outbox;
using StackExchange.Redis;

namespace SecurityService.Infrastructure.Services.Outbox;

public class OutboxService : IOutboxService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<OutboxService> _logger;

    private const int MaxRetries = 3;
    private static readonly int[] BackoffSecondsPerRetry = [1, 2, 4];

    public OutboxService(IConnectionMultiplexer redis, ILogger<OutboxService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishAsync(string queueKey, string serializedMessage, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                await db.ListLeftPushAsync(queueKey, serializedMessage);
                _logger.LogInformation("Message published to {QueueKey}", queueKey);
                return;
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish to {QueueKey} (attempt {Attempt}/{MaxRetries})",
                    queueKey, attempt + 1, MaxRetries);

                if (attempt < MaxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(BackoffSecondsPerRetry[attempt]), ct);
                }
            }
        }

        // All retries exhausted — push to dead-letter queue
        var serviceName = ExtractServiceName(queueKey);
        var dlqKey = $"dlq:{serviceName}";

        try
        {
            await db.ListLeftPushAsync(dlqKey, serializedMessage);
            _logger.LogError(
                "Message moved to dead-letter queue {DlqKey} after {MaxRetries} failed attempts on {QueueKey}",
                dlqKey, MaxRetries, queueKey);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex,
                "Failed to publish to dead-letter queue {DlqKey}. Message lost for {QueueKey}",
                dlqKey, queueKey);
        }
    }

    private static string ExtractServiceName(string queueKey)
    {
        // Expected pattern: "outbox:{service}"
        var parts = queueKey.Split(':');
        return parts.Length >= 2 ? parts[1] : "unknown";
    }
}
