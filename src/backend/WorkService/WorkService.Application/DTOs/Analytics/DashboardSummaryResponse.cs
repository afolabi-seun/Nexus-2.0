namespace WorkService.Application.DTOs.Analytics;

public class DashboardSummaryResponse
{
    public ProjectHealthResponse? ProjectHealth { get; set; }
    public VelocitySnapshotResponse? VelocitySnapshot { get; set; }
    public int ActiveBugCount { get; set; }
    public int ActiveRiskCount { get; set; }
    public int BlockedStoryCount { get; set; }
    public decimal TotalProjectCost { get; set; }
    public decimal BurnRatePerDay { get; set; }
}
