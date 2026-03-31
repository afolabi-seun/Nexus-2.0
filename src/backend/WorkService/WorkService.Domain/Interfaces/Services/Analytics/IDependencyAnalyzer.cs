using WorkService.Domain.Entities;

namespace WorkService.Domain.Interfaces.Services.Analytics;

public interface IDependencyAnalyzer
{
    /// <summary>
    /// Analyzes story dependencies from StoryLink records.
    /// Returns blocking chains, blocked stories, and circular dependencies.
    /// </summary>
    object Analyze(
        IEnumerable<StoryLink> links,
        IEnumerable<Story> stories,
        Guid? filterSprintId = null);
}
