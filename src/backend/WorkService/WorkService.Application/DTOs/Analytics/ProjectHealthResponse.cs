namespace WorkService.Application.DTOs.Analytics;

public class ProjectHealthResponse
{
    public decimal OverallScore { get; set; }
    public decimal VelocityScore { get; set; }
    public decimal BugRateScore { get; set; }
    public decimal OverdueScore { get; set; }
    public decimal RiskScore { get; set; }
    public string Trend { get; set; } = "stable";
    public DateTime SnapshotDate { get; set; }
    public List<ProjectHealthResponse>? History { get; set; }
}
