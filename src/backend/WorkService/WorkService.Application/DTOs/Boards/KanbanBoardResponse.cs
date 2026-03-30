using WorkService.Application.DTOs.Labels;

namespace WorkService.Application.DTOs.Boards;

public class KanbanBoardResponse
{
    public List<KanbanColumn> Columns { get; set; } = new();
}

public class KanbanColumn
{
    public string Status { get; set; } = string.Empty;
    public int CardCount { get; set; }
    public int TotalPoints { get; set; }
    public List<KanbanCard> Cards { get; set; } = new();
}

public class KanbanCard
{
    public Guid StoryId { get; set; }
    public string StoryKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int? StoryPoints { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeAvatarUrl { get; set; }
    public List<LabelResponse> Labels { get; set; } = new();
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
