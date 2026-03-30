using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Services.BackgroundServices;

public class UsagePersistenceHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UsagePersistenceHostedService> _logger;

    public UsagePersistenceHostedService(IServiceScopeFactory scopeFactory, ILogger<UsagePersistenceHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PersistUsageCounters(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting usage counters");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task PersistUsageCounters(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var usageRecordRepo = scope.ServiceProvider.GetRequiredService<IUsageRecordRepository>();
        var server = redis.GetServer(redis.GetEndPoints().First());
        var db = redis.GetDatabase();

        var pattern = "usage:*";
        var keys = server.Keys(pattern: pattern).ToList();

        foreach (var key in keys)
        {
            try
            {
                var parts = key.ToString().Split(':');
                if (parts.Length != 3) continue;

                var orgIdStr = parts[1];
                var metricName = parts[2];

                if (!Guid.TryParse(orgIdStr, out var orgId)) continue;
                if (!MetricName.IsValid(metricName)) continue;

                var val = await db.StringGetAsync(key);
                if (!val.HasValue || !long.TryParse(val, out var metricValue)) continue;

                var now = DateTime.UtcNow;
                var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var periodEnd = periodStart.AddMonths(1);

                await usageRecordRepo.UpsertAsync(new UsageRecord
                {
                    OrganizationId = orgId,
                    MetricName = metricName,
                    MetricValue = metricValue,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist usage counter for key {Key}", key);
            }
        }
    }
}
