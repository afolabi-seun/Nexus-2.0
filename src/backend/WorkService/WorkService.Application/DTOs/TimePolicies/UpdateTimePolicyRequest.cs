namespace WorkService.Application.DTOs.TimePolicies;

public class UpdateTimePolicyRequest
{
    public decimal RequiredHoursPerDay { get; set; }
    public decimal OvertimeThresholdHoursPerDay { get; set; }
    public bool ApprovalRequired { get; set; }
    public string ApprovalWorkflow { get; set; } = string.Empty;
    public decimal MaxDailyHours { get; set; }
}
