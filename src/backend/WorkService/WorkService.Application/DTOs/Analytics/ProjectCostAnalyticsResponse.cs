using WorkService.Application.DTOs.TimeEntries;

namespace WorkService.Application.DTOs.Analytics;

public class ProjectCostAnalyticsResponse
{
    public decimal TotalCost { get; set; }
    public decimal TotalBillableHours { get; set; }
    public decimal TotalNonBillableHours { get; set; }
    public decimal BurnRatePerDay { get; set; }
    public List<MemberCostDetail> CostByMember { get; set; } = new();
    public List<DepartmentCostDetail> CostByDepartment { get; set; } = new();
    public List<CostTrendItem> CostTrend { get; set; } = new();
}
