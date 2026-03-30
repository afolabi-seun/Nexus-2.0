namespace WorkService.Application.DTOs.Sprints;

public class CreateSprintRequest
{
    public string SprintName { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
