namespace WorkService.Application.DTOs.Sprints;

public class SprintListResponse
{
    public Guid SprintId { get; set; }
    public string SprintName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? Velocity { get; set; }
    public int StoryCount { get; set; }
}
