namespace WorkService.Application.DTOs.TimeEntries;

public class UpdateTimeEntryRequest
{
    public int? DurationMinutes { get; set; }
    public DateTime? Date { get; set; }
    public bool? IsBillable { get; set; }
    public string? Notes { get; set; }
}
