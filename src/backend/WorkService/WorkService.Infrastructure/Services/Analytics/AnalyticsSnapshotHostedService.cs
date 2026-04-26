using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkService.Application.DTOs.Analytics;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Services.Analytics;
using WorkService.Domain.Results;
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

    public async Task<ServiceResult<object>> TriggerSprintCloseSnapshotsAsync(Guid sprintId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        var sprintRepo = scope.ServiceProvider.GetRequiredService<ISprintRepository>();

        var sprint = await sprintRepo.GetByIdAsync(sprintId, ct);
        if (sprint == null) return ServiceResult<object>.NoContent("Sprint not found.");

        var projectId = sprint.ProjectId;

        await analyticsService.GenerateVelocitySnapshotAsync(sprintId, ct);
        _logger.LogInformation("Generated velocity snapshot for sprint {SprintId}", sprintId);

        await analyticsService.GenerateHealthSnapshotAsync(projectId, ct);
        _logger.LogInformation("Generated health snapshot for project {ProjectId}", projectId);

        await analyticsService.GenerateResourceAllocationSnapshotAsync(
            projectId, sprint.StartDate, sprint.EndDate, ct);
        _logger.LogInformation("Generated resource allocation snapshot for project {ProjectId}", projectId);

        return ServiceResult<object>.NoContent("Sprint close snapshots generated.");
    }

    public async Task<ServiceResult<object>> GeneratePeriodicSnapshotsAsync(CancellationToken ct = default)
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

        return ServiceResult<object>.NoContent("Periodic snapshots generated.");
    }

    public async Task<ServiceResult<object>> GetSnapshotStatusAsync(CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(RedisKeys.AnalyticsSnapshotStatus);

        if (cached.HasValue)
        {
            var status = JsonSerializer.Deserialize<SnapshotStatusResponse>(cached!);
            if (status != null) return ServiceResult<object>.Ok(status, "Snapshot status retrieved.");
        }

        return ServiceResult<object>.Ok(new SnapshotStatusResponse
        {
            LastRunTime = null,
            ProjectsProcessed = 0,
            ErrorsEncountered = 0,
            NextScheduledRun = null
        }, "Snapshot status retrieved.");
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
