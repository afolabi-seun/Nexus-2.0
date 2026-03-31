namespace WorkService.Application.DTOs.TimeEntries;

public class SprintVelocityResponse
{
    public int TotalStoryPoints { get; set; }
    public decimal TotalLoggedHours { get; set; }
    public decimal? AverageHoursPerPoint { get; set; }
    public int CompletedStoryCount { get; set; }
}
