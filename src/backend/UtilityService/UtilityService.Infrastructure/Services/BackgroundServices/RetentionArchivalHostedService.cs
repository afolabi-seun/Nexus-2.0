using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UtilityService.Domain.Entities;
using UtilityService.Infrastructure.Configuration;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Services.BackgroundServices;

public class RetentionArchivalHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppSettings _appSettings;
    private readonly ILogger<RetentionArchivalHostedService> _logger;
    private const int BatchSize = 500;

    public RetentionArchivalHostedService(
        IServiceScopeFactory scopeFactory, AppSettings appSettings,
        ILogger<RetentionArchivalHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _appSettings = appSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddHours(_appSettings.RetentionScheduleHour);
            if (nextRun <= now) nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await RunArchivalAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retention archival failed.");
            }
        }
    }

    private async Task RunArchivalAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UtilityDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_appSettings.RetentionPeriodDays);
        _logger.LogInformation("Starting retention archival. CutoffDate={CutoffDate}", cutoffDate);

        while (true)
        {
            var batch = await context.AuditLogs.IgnoreQueryFilters()
                .Where(a => a.DateCreated < cutoffDate)
                .Take(BatchSize).ToListAsync(ct);

            if (batch.Count == 0) break;

            var archived = batch.Select(a => new ArchivedAuditLog
            {
                OrganizationId = a.OrganizationId, ServiceName = a.ServiceName,
                Action = a.Action, EntityType = a.EntityType, EntityId = a.EntityId,
                UserId = a.UserId, OldValue = a.OldValue, NewValue = a.NewValue,
                IpAddress = a.IpAddress, CorrelationId = a.CorrelationId,
                DateCreated = a.DateCreated, ArchivedDate = DateTime.UtcNow
            }).ToList();

            context.ArchivedAuditLogs.AddRange(archived);
            context.AuditLogs.RemoveRange(batch);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation("Archived {Count} audit logs.", batch.Count);
        }
    }
}
