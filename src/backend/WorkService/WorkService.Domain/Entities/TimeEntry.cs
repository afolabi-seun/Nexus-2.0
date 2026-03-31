using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class TimeEntry : IOrganizationEntity
{
    public Guid TimeEntryId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid StoryId { get; set; }
    public Guid MemberId { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime Date { get; set; }
    public bool IsBillable { get; set; } = true;
    public bool IsOvertime { get; set; } = false;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string? Notes { get; set; }
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
