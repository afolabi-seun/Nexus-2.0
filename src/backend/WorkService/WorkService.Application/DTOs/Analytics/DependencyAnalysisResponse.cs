namespace WorkService.Application.DTOs.Analytics;

public class DependencyAnalysisResponse
{
    public int TotalDependencies { get; set; }
    public List<DependencyChain> BlockingChains { get; set; } = new();
    public List<BlockedStoryDetail> BlockedStories { get; set; } = new();
    public List<List<Guid>> CircularDependencies { get; set; } = new();
}
