namespace WorkService.Application.DTOs.Reports;

public class DepartmentWorkloadResponse
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int MemberCount { get; set; }
    public decimal AvgTasksPerMember { get; set; }
}
