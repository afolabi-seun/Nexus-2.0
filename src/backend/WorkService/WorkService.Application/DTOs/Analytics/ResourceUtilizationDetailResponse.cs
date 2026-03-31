namespace WorkService.Application.DTOs.Analytics;

public class ResourceUtilizationDetailResponse
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal TotalLoggedHours { get; set; }
    public decimal ExpectedHours { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public decimal BillableHours { get; set; }
    public decimal NonBillableHours { get; set; }
    public decimal OvertimeHours { get; set; }
}
