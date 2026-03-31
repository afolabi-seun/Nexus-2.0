using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class TimeApproval : IOrganizationEntity
{
    public Guid TimeApprovalId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid TimeEntryId { get; set; }
    public Guid ApproverId { get; set; }
    public string Action { get; set; } = string.Empty; // Approved, Rejected
    public string? Reason { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
