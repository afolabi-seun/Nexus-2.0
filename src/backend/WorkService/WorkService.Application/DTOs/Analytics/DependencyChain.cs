namespace WorkService.Application.DTOs.Analytics;

public class DependencyChain
{
    public int ChainLength { get; set; }
    public List<ChainStoryDetail> Stories { get; set; } = new();
    public bool CriticalPath { get; set; }
}

public class ChainStoryDetail
{
    public Guid StoryId { get; set; }
    public string StoryKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? AssigneeId { get; set; }
}
