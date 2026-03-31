namespace WorkService.Application.DTOs.Analytics;

public class BlockedStoryDetail
{
    public Guid StoryId { get; set; }
    public string StoryKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<Guid> BlockedByStoryIds { get; set; } = new();
}
