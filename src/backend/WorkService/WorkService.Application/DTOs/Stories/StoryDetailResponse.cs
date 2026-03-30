using WorkService.Application.DTOs.Labels;
using WorkService.Application.DTOs.Tasks;

namespace WorkService.Application.DTOs.Stories;

public class StoryDetailResponse
{
    public Guid StoryId { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string StoryKey { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public int? StoryPoints { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public string? AssigneeAvatarUrl { get; set; }
    public Guid ReporterId { get; set; }
    public string? ReporterName { get; set; }
    public Guid? SprintId { get; set; }
    public string? SprintName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int TotalTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<DepartmentContribution> DepartmentContributions { get; set; } = new();
    public List<TaskDetailResponse> Tasks { get; set; } = new();
    public List<LabelResponse> Labels { get; set; } = new();
    public List<StoryLinkResponse> Links { get; set; } = new();
    public int CommentCount { get; set; }
    public string FlgStatus { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}

public class DepartmentContribution
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
}

public class StoryLinkResponse
{
    public Guid LinkId { get; set; }
    public Guid TargetStoryId { get; set; }
    public string TargetStoryKey { get; set; } = string.Empty;
    public string TargetStoryTitle { get; set; } = string.Empty;
    public string LinkType { get; set; } = string.Empty;
}
