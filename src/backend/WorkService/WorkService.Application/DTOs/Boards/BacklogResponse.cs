using WorkService.Application.DTOs.Labels;

namespace WorkService.Application.DTOs.Boards;

public class BacklogResponse
{
    public int TotalStories { get; set; }
    public int TotalPoints { get; set; }
    public List<BacklogItem> Items { get; set; } = new();
}

public class BacklogItem
{
    public Guid StoryId { get; set; }
    public string StoryKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int? StoryPoints { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public List<LabelResponse> Labels { get; set; } = new();
    public int TaskCount { get; set; }
    public DateTime DateCreated { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
