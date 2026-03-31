namespace WorkService.Domain.Interfaces.Services.Analytics;

public interface IAnalyticsService
{
    // Velocity
    Task<object> GetVelocityTrendsAsync(Guid projectId, int sprintCount, CancellationToken ct = default);
    Task GenerateVelocitySnapshotAsync(Guid sprintId, CancellationToken ct = default);

    // Resource Management
    Task<object> GetResourceManagementAsync(Guid orgId, DateTime? dateFrom, DateTime? dateTo, Guid? departmentId, CancellationToken ct = default);

    // Resource Utilization
    Task<object> GetResourceUtilizationAsync(Guid projectId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task GenerateResourceAllocationSnapshotAsync(Guid projectId, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);

    // Project Cost
    Task<object> GetProjectCostAnalyticsAsync(Guid projectId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);

    // Project Health
    Task<object> GetProjectHealthAsync(Guid projectId, bool includeHistory, CancellationToken ct = default);
    Task GenerateHealthSnapshotAsync(Guid projectId, CancellationToken ct = default);

    // Bug Metrics
    Task<object> GetBugMetricsAsync(Guid projectId, Guid? sprintId, CancellationToken ct = default);

    // Dashboard
    Task<object> GetDashboardAsync(Guid projectId, CancellationToken ct = default);

    // Snapshot Status
    Task<object> GetSnapshotStatusAsync(CancellationToken ct = default);
}
