namespace WorkService.Domain.Interfaces.Services.Analytics;

public interface IAnalyticsSnapshotService
{
    /// <summary>
    /// Triggered when a sprint is completed. Generates velocity, health, and resource snapshots.
    /// </summary>
    Task TriggerSprintCloseSnapshotsAsync(Guid sprintId, CancellationToken ct = default);

    /// <summary>
    /// Periodic generation of health and resource snapshots for all active projects.
    /// </summary>
    Task GeneratePeriodicSnapshotsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the current snapshot generation status metadata.
    /// </summary>
    Task<object> GetSnapshotStatusAsync(CancellationToken ct = default);
}
