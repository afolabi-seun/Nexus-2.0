namespace WorkService.Application.DTOs.TimeEntries;

public class TimeEntryResponse
{
    public Guid TimeEntryId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid StoryId { get; set; }
    public Guid MemberId { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime Date { get; set; }
    public bool IsBillable { get; set; }
    public bool IsOvertime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string FlgStatus { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
