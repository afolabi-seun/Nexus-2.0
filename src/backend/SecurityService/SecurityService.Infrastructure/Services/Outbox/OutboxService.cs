using Microsoft.Extensions.Logging;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Infrastructure.Redis;
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

        try
        {
            await db.ListLeftPushAsync(RedisKeys.Dlq, serializedMessage);
            _logger.LogError(
                "Message moved to dead-letter queue {DlqKey} after {MaxRetries} failed attempts on {QueueKey}",
                RedisKeys.Dlq, MaxRetries, queueKey);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex,
                "Failed to publish to dead-letter queue {DlqKey}. Message lost for {QueueKey}",
                RedisKeys.Dlq, queueKey);
        }
    }
}
