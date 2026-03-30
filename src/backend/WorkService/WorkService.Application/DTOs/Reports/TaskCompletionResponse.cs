namespace WorkService.Application.DTOs.Reports;

public class TaskCompletionResponse
{
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AvgCompletionTimeHours { get; set; }
}
