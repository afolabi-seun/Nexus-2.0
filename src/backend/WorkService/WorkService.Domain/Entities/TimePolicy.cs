using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class TimePolicy : IOrganizationEntity
{
    public Guid TimePolicyId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public decimal RequiredHoursPerDay { get; set; } = 8m;
    public decimal OvertimeThresholdHoursPerDay { get; set; } = 10m;
    public bool ApprovalRequired { get; set; } = false;
    public string ApprovalWorkflow { get; set; } = "None"; // None, DeptLeadApproval, ProjectLeadApproval
    public decimal MaxDailyHours { get; set; } = 24m;
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
