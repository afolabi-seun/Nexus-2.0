namespace WorkService.Application.DTOs.TimeEntries;

public class CreateTimeEntryRequest
{
    public Guid StoryId { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime Date { get; set; }
    public bool IsBillable { get; set; } = true;
    public string? Notes { get; set; }
}
