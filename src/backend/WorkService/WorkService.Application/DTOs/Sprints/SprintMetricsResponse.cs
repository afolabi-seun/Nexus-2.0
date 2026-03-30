namespace WorkService.Application.DTOs.Sprints;

public class SprintMetricsResponse
{
    public int TotalStories { get; set; }
    public int CompletedStories { get; set; }
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public decimal CompletionRate { get; set; }
    public int Velocity { get; set; }
    public Dictionary<string, int> StoriesByStatus { get; set; } = new();
    public Dictionary<string, int> TasksByDepartment { get; set; } = new();
    public List<BurndownDataPoint> BurndownData { get; set; } = new();
}
