using WorkService.Application.DTOs.Labels;

namespace WorkService.Application.DTOs.Stories;

public class StoryListResponse
{
    public Guid StoryId { get; set; }
    public string StoryKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string StoryType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? StoryPoints { get; set; }
    public string? AssigneeName { get; set; }
    public string? SprintName { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public List<LabelResponse> Labels { get; set; } = new();
    public DateTime? DueDate { get; set; }
    public DateTime DateCreated { get; set; }
}
