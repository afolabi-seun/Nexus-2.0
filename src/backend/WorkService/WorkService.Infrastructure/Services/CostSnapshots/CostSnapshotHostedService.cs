using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.CostSnapshots;
using WorkService.Application.DTOs.TimeEntries;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.CostSnapshots;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Services.CostSnapshots;
using WorkService.Domain.Interfaces.Services.TimeEntries;

namespace WorkService.Infrastructure.Services.CostSnapshots;

public class CostSnapshotHostedService : BackgroundService, ICostSnapshotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CostSnapshotHostedService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6);

    public CostSnapshotHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CostSnapshotHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<object> ListByProjectAsync(Guid projectId, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var snapshotRepo = scope.ServiceProvider.GetRequiredService<ICostSnapshotRepository>();

        var (items, totalCount) = await snapshotRepo.ListByProjectAsync(
            projectId, dateFrom, dateTo, page, pageSize, ct);

        var responses = items.Select(MapToResponse).ToList();

        return new PaginatedResponse<CostSnapshotResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CostSnapshotHostedService started with {Interval}h interval", _interval.TotalHours);

        using var timer = new PeriodicTimer(_interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateSnapshotsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating cost snapshots. Will retry on next interval.");
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

        _logger.LogInformation("CostSnapshotHostedService stopped");
    }

    private async System.Threading.Tasks.Task GenerateSnapshotsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var projectRepo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var timeEntryService = scope.ServiceProvider.GetRequiredService<ITimeEntryService>();
        var snapshotRepo = scope.ServiceProvider.GetRequiredService<ICostSnapshotRepository>();

        // Query all active projects across all organizations
        // We use a large page size to get all projects; in production this would be batched
        var (projects, _) = await projectRepo.ListAsync(Guid.Empty, 1, int.MaxValue, "Active", ct);

        var periodEnd = DateTime.UtcNow;
        var periodStart = periodEnd.AddHours(-_interval.TotalHours);

        foreach (var project in projects)
        {
            try
            {
                var costSummary = await timeEntryService.GetProjectCostSummaryAsync(
                    project.ProjectId, null, null, ct);

                var summary = (ProjectCostSummaryResponse)costSummary;

                var snapshot = new CostSnapshot
                {
                    OrganizationId = project.OrganizationId,
                    ProjectId = project.ProjectId,
                    TotalCost = summary.TotalCost,
                    TotalBillableHours = summary.TotalBillableHours,
                    TotalNonBillableHours = summary.TotalNonBillableHours,
                    SnapshotDate = DateTime.UtcNow,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd
                };

                await snapshotRepo.AddOrUpdateAsync(snapshot, ct);

                _logger.LogDebug("Generated cost snapshot for project {ProjectId}", project.ProjectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate snapshot for project {ProjectId}", project.ProjectId);
            }
        }
    }

    private static CostSnapshotResponse MapToResponse(CostSnapshot snapshot)
    {
        return new CostSnapshotResponse
        {
            CostSnapshotId = snapshot.CostSnapshotId,
            OrganizationId = snapshot.OrganizationId,
            ProjectId = snapshot.ProjectId,
            TotalCost = snapshot.TotalCost,
            TotalBillableHours = snapshot.TotalBillableHours,
            TotalNonBillableHours = snapshot.TotalNonBillableHours,
            SnapshotDate = snapshot.SnapshotDate,
            PeriodStart = snapshot.PeriodStart,
            PeriodEnd = snapshot.PeriodEnd,
            DateCreated = snapshot.DateCreated
        };
    }
}
