using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UtilityService.Application.DTOs;
using UtilityService.Domain.Interfaces.Services.Outbox;
using UtilityService.Infrastructure.Configuration;

namespace UtilityService.Infrastructure.Services.BackgroundServices;

public class OutboxProcessorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;
    private readonly ILogger<OutboxProcessorHostedService> _logger;

    private static readonly string[] OutboxQueues = { "outbox:security", "outbox:profile", "outbox:work" };

    public OutboxProcessorHostedService(
        IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis,
        AppSettings appSettings, ILogger<OutboxProcessorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _appSettings = appSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var queue in OutboxQueues)
            {
                await ProcessQueueAsync(queue, stoppingToken);
            }
            await Task.Delay(TimeSpan.FromSeconds(_appSettings.OutboxPollIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessQueueAsync(string queue, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        while (true)
        {
            var message = await db.ListRightPopAsync(queue);
            if (message.IsNullOrEmpty) break;

            using var scope = _scopeFactory.CreateScope();
            var router = scope.ServiceProvider.GetRequiredService<IOutboxMessageRouter>();
            try
            {
                await router.RouteAsync(message!, queue, ct);
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(db, queue, message!, ex);
            }
        }
    }

    private async Task HandleFailureAsync(IDatabase db, string queue, string message, Exception ex)
    {
        OutboxMessage? outboxMessage;
        try
        {
            outboxMessage = JsonSerializer.Deserialize<OutboxMessage>(message);
        }
        catch
        {
            var dlqKey = queue.Replace("outbox:", "dlq:");
            await db.ListLeftPushAsync(dlqKey, message);
            return;
        }

        if (outboxMessage == null)
        {
            var dlqKey = queue.Replace("outbox:", "dlq:");
            await db.ListLeftPushAsync(dlqKey, message);
            return;
        }

        outboxMessage.RetryCount++;
        if (outboxMessage.RetryCount >= 3)
        {
            var dlqKey = queue.Replace("outbox:", "dlq:");
            await db.ListLeftPushAsync(dlqKey, JsonSerializer.Serialize(outboxMessage));
            _logger.LogWarning("Message moved to DLQ. Queue={Queue} MessageId={MessageId} RetryCount={RetryCount} Error={Error}",
                queue, outboxMessage.Id, outboxMessage.RetryCount, ex.Message);
        }
        else
        {
            await db.ListLeftPushAsync(queue, JsonSerializer.Serialize(outboxMessage));
        }
    }
}
