namespace WorkService.Application.DTOs.Analytics;

public class VelocitySnapshotResponse
{
    public Guid SprintId { get; set; }
    public string SprintName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CommittedPoints { get; set; }
    public int CompletedPoints { get; set; }
    public decimal TotalLoggedHours { get; set; }
    public decimal? AverageHoursPerPoint { get; set; }
    public int CompletedStoryCount { get; set; }
}
