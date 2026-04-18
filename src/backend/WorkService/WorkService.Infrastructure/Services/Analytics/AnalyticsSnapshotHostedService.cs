using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkService.Application.DTOs.Analytics;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Services.Analytics;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.Analytics;

public class AnalyticsSnapshotHostedService : BackgroundService, IAnalyticsSnapshotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AnalyticsSnapshotHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    public AnalyticsSnapshotHostedService(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        ILogger<AnalyticsSnapshotHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task TriggerSprintCloseSnapshotsAsync(Guid sprintId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        var sprintRepo = scope.ServiceProvider.GetRequiredService<ISprintRepository>();

        var sprint = await sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null) return;

        var projectId = sprint.ProjectId;

        // Generate velocity snapshot
        await analyticsService.GenerateVelocitySnapshotAsync(sprintId, ct);
        _logger.LogInformation("Generated velocity snapshot for sprint {SprintId}", sprintId);

        // Generate health snapshot
        await analyticsService.GenerateHealthSnapshotAsync(projectId, ct);
        _logger.LogInformation("Generated health snapshot for project {ProjectId}", projectId);

        // Generate resource allocation snapshot
        await analyticsService.GenerateResourceAllocationSnapshotAsync(
            projectId, sprint.StartDate, sprint.EndDate, ct);
        _logger.LogInformation("Generated resource allocation snapshot for project {ProjectId}", projectId);
    }

    public async System.Threading.Tasks.Task GeneratePeriodicSnapshotsAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var projectRepo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

        var (projects, _) = await projectRepo.ListAsync(Guid.Empty, 1, int.MaxValue, "Active", ct);
        var projectList = projects.ToList();

        var errorsEncountered = 0;
        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd.AddHours(-_interval.TotalHours);

        foreach (var project in projectList)
        {
            try
            {
                await analyticsService.GenerateHealthSnapshotAsync(project.ProjectId, ct);
                await analyticsService.GenerateResourceAllocationSnapshotAsync(
                    project.ProjectId, periodStart, periodEnd, ct);
            }
            catch (Exception ex)
            {
                errorsEncountered++;
                _logger.LogError(ex, "Failed to generate periodic snapshots for project {ProjectId}", project.ProjectId);
            }
        }

        // Store status in Redis with 24h TTL
        var status = new SnapshotStatusResponse
        {
            LastRunTime = DateTime.UtcNow,
            ProjectsProcessed = projectList.Count,
            ErrorsEncountered = errorsEncountered,
            NextScheduledRun = DateTime.UtcNow.Add(_interval)
        };

        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(status);
        await db.StringSetAsync(RedisKeys.AnalyticsSnapshotStatus, json, TimeSpan.FromHours(24));
    }

    public async Task<object> GetSnapshotStatusAsync(CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(RedisKeys.AnalyticsSnapshotStatus);

        if (cached.HasValue)
        {
            var status = JsonSerializer.Deserialize<SnapshotStatusResponse>(cached!);
            if (status != null) return status;
        }

        return new SnapshotStatusResponse
        {
            LastRunTime = null,
            ProjectsProcessed = 0,
            ErrorsEncountered = 0,
            NextScheduledRun = null
        };
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalyticsSnapshotHostedService started with {Interval}h interval", _interval.TotalHours);

        using var timer = new PeriodicTimer(_interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GeneratePeriodicSnapshotsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating analytics snapshots. Will retry on next interval.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("AnalyticsSnapshotHostedService stopped");
    }
}
