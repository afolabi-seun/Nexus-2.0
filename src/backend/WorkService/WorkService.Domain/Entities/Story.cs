using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class Story : IOrganizationEntity
{
    public Guid StoryId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public string StoryKey { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AcceptanceCriteria { get; set; }
    public int? StoryPoints { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Backlog";
    public Guid? AssigneeId { get; set; }
    public Guid ReporterId { get; set; }
    public Guid? SprintId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string FlgStatus { get; set; } = "A";
    public string? SearchVector { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
