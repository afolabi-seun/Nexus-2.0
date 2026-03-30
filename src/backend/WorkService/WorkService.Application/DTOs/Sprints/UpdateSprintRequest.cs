namespace WorkService.Application.DTOs.Sprints;

public class UpdateSprintRequest
{
    public string? SprintName { get; set; }
    public string? Goal { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
