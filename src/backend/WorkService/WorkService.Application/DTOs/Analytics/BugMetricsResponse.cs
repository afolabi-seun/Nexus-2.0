namespace WorkService.Application.DTOs.Analytics;

public class BugMetricsResponse
{
    public int TotalBugs { get; set; }
    public int OpenBugs { get; set; }
    public int ClosedBugs { get; set; }
    public int ReopenedBugs { get; set; }
    public decimal BugRate { get; set; }
    public Dictionary<string, int> BugsBySeverity { get; set; } = new();
    public List<BugTrendItem> BugTrend { get; set; } = new();
}
