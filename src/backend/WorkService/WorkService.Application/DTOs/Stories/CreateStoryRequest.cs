namespace WorkService.Application.DTOs.Stories;

public class CreateStoryRequest
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public int? StoryPoints { get; set; }
    public string Priority { get; set; } = "Medium";
    public Guid? DepartmentId { get; set; }
    public DateTime? DueDate { get; set; }
}
