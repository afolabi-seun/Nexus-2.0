using WorkService.Application.DTOs.Stories;

namespace WorkService.Application.DTOs.Sprints;

public class SprintDetailResponse
{
    public Guid SprintId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string SprintName { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? Velocity { get; set; }
    public List<StoryListResponse> Stories { get; set; } = new();
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
