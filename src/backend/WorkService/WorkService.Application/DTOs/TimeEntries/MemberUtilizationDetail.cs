namespace WorkService.Application.DTOs.TimeEntries;

public class MemberUtilizationDetail
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal TotalLoggedHours { get; set; }
    public decimal ExpectedHours { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public decimal BillableHours { get; set; }
    public decimal NonBillableHours { get; set; }
}
