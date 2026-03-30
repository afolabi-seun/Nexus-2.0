namespace WorkService.Application.DTOs.Reports;

public class CycleTimeResponse
{
    public string StoryKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CycleTimeDays { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime CompletedDate { get; set; }
}
