namespace WorkService.Application.DTOs.Analytics;

public class HealthScoreResult
{
    public decimal OverallScore { get; set; }
    public decimal VelocityScore { get; set; }
    public decimal BugRateScore { get; set; }
    public decimal OverdueScore { get; set; }
    public decimal RiskScore { get; set; }
    public string Trend { get; set; } = "stable";
}
