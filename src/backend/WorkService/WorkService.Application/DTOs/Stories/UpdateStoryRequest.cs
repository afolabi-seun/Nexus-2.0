namespace WorkService.Application.DTOs.Stories;

public class UpdateStoryRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public int? StoryPoints { get; set; }
    public string? Priority { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? DueDate { get; set; }
}
