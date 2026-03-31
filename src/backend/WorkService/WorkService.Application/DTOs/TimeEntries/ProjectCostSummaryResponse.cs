namespace WorkService.Application.DTOs.TimeEntries;

public class ProjectCostSummaryResponse
{
    public decimal TotalCost { get; set; }
    public decimal TotalBillableHours { get; set; }
    public decimal TotalNonBillableHours { get; set; }
    public List<MemberCostDetail> CostByMember { get; set; } = new();
    public List<DepartmentCostDetail> CostByDepartment { get; set; } = new();
}
