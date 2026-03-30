namespace WorkService.Application.DTOs.Tasks;

public class CreateTaskRequest
{
    public Guid StoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public decimal? EstimatedHours { get; set; }
    public DateTime? DueDate { get; set; }
}
