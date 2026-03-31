namespace WorkService.Application.DTOs.Analytics;

public class ResourceManagementResponse
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public decimal TotalLoggedHours { get; set; }
    public List<ProjectBreakdownItem> ProjectBreakdown { get; set; } = new();
    public decimal CapacityUtilizationPercentage { get; set; }
}
