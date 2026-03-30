namespace WorkService.Application.DTOs.Reports;

public class CapacityUtilizationResponse
{
    public string MemberName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int ActiveTasks { get; set; }
    public int MaxConcurrentTasks { get; set; }
    public decimal UtilizationRate { get; set; }
    public string Availability { get; set; } = string.Empty;
}
