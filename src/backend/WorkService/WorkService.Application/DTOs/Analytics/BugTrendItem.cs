namespace WorkService.Application.DTOs.Analytics;

public class BugTrendItem
{
    public Guid SprintId { get; set; }
    public string SprintName { get; set; } = string.Empty;
    public int BugCount { get; set; }
}
