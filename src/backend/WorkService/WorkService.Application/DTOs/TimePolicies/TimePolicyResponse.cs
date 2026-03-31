namespace WorkService.Application.DTOs.TimePolicies;

public class TimePolicyResponse
{
    public Guid TimePolicyId { get; set; }
    public Guid OrganizationId { get; set; }
    public decimal RequiredHoursPerDay { get; set; }
    public decimal OvertimeThresholdHoursPerDay { get; set; }
    public bool ApprovalRequired { get; set; }
    public string ApprovalWorkflow { get; set; } = string.Empty;
    public decimal MaxDailyHours { get; set; }
    public string FlgStatus { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
