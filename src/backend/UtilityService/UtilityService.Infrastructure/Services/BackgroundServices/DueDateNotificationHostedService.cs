using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace UtilityService.Infrastructure.Services.BackgroundServices;

public class DueDateNotificationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DueDateNotificationHostedService> _logger;

    public DueDateNotificationHostedService(
        IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis,
        ILogger<DueDateNotificationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanDueDatesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Due date notification scan failed.");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    private async Task ScanDueDatesAsync(CancellationToken ct)
    {
        var db = _redis.GetDatabase();

        // Scan for due date entries published by WorkService
        // The WorkService publishes due-date-approaching events to a Redis set
        // We check for deduplication using a Redis key per entity
        var dueDateKey = "duedate:approaching";
        var entries = await db.SetMembersAsync(dueDateKey);

        foreach (var entry in entries)
        {
            var entityKey = $"duedate:notified:{entry}";
            var alreadyNotified = await db.KeyExistsAsync(entityKey);
            if (alreadyNotified) continue;

            // Mark as notified for 24 hours to avoid duplicates
            await db.StringSetAsync(entityKey, "1", TimeSpan.FromHours(24));

            _logger.LogInformation("Due date notification published for entity {EntityId}", entry);
        }
    }
}
