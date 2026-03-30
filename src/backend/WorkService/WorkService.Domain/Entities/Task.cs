using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class Task : IOrganizationEntity
{
    public Guid TaskId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid StoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Status { get; set; } = "ToDo";
    public string Priority { get; set; } = "Medium";
    public Guid? AssigneeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; } = 0m;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string FlgStatus { get; set; } = "A";
    public NpgsqlTypes.NpgsqlTsVector? SearchVector { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
