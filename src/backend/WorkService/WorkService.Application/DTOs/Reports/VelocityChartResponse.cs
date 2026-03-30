namespace WorkService.Application.DTOs.Reports;

public class VelocityChartResponse
{
    public string SprintName { get; set; } = string.Empty;
    public int Velocity { get; set; }
    public int TotalStoryPoints { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
