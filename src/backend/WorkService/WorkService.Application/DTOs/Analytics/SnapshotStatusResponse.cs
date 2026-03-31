namespace WorkService.Application.DTOs.Analytics;

public class SnapshotStatusResponse
{
    public DateTime? LastRunTime { get; set; }
    public int ProjectsProcessed { get; set; }
    public int ErrorsEncountered { get; set; }
    public DateTime? NextScheduledRun { get; set; }
}
