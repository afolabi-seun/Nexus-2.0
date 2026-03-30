namespace WorkService.Application.DTOs.Boards;

public class SprintBoardResponse
{
    public string? SprintName { get; set; }
    public bool HasActiveSprint { get; set; }
    public string? Message { get; set; }
    public string? ProjectName { get; set; }
    public List<SprintBoardColumn> Columns { get; set; } = new();
}

public class SprintBoardColumn
{
    public string Status { get; set; } = string.Empty;
    public List<SprintBoardCard> Cards { get; set; } = new();
}

public class SprintBoardCard
{
    public Guid TaskId { get; set; }
    public string StoryKey { get; set; } = string.Empty;
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public string? DepartmentName { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
}
