namespace WorkService.Application.DTOs.TimeEntries;

public class TimerStatusResponse
{
    public Guid StoryId { get; set; }
    public DateTime StartTime { get; set; }
    public long ElapsedSeconds { get; set; }
}
