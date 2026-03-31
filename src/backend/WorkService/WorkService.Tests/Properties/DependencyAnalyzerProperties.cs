using FsCheck;
using FsCheck.Xunit;
using WorkService.Application.DTOs.Analytics;
using WorkService.Domain.Entities;
using WorkService.Infrastructure.Services.Analytics;

namespace WorkService.Tests.Properties;

public static class DependencyGenerator
{
    private static readonly string[] Statuses = { "Backlog", "In Progress", "In Review", "Done" };

    public static Story CreateStory(Guid id, string status = "In Progress") => new()
    {
        StoryId = id,
        OrganizationId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        StoryKey = $"STORY-{id.ToString()[..4]}",
        Title = $"Story {id.ToString()[..4]}",
        Status = status,
        ReporterId = Guid.NewGuid()
    };

    public static StoryLink CreateLink(Guid source, Guid target, string linkType = "blocks") => new()
    {
        StoryLinkId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        SourceStoryId = source,
        TargetStoryId = target,
        LinkType = linkType
    };

    public static string RandomStatus(int seed) => Statuses[Math.Abs(seed) % Statuses.Length];
}

public class DependencyAnalyzerProperties
{
    private readonly DependencyAnalyzer _sut = new();

    // Feature: analytics-reporting, Property 1: Empty links produce empty analysis (totalDependencies=0, empty chains, empty blocked, empty cycles)
    [Property(MaxTest = 100)]
    public bool EmptyLinks_ProduceEmptyAnalysis()
    {
        var result = (DependencyAnalysisResponse)_sut.Analyze(
            Enumerable.Empty<StoryLink>(),
            Enumerable.Empty<Story>());

        return result.TotalDependencies == 0
            && result.BlockingChains.Count == 0
            && result.BlockedStories.Count == 0
            && result.CircularDependencies.Count == 0;
    }

    // Feature: analytics-reporting, Property 2: A simple chain A→B→C produces one chain of length 3
    [Property(MaxTest = 100)]
    public bool SimpleChain_ProducesOneChainOfLength3()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var stories = new[]
        {
            DependencyGenerator.CreateStory(a),
            DependencyGenerator.CreateStory(b),
            DependencyGenerator.CreateStory(c)
        };

        var links = new[]
        {
            DependencyGenerator.CreateLink(a, b, "blocks"),
            DependencyGenerator.CreateLink(b, c, "blocks")
        };

        var result = (DependencyAnalysisResponse)_sut.Analyze(links, stories);

        return result.BlockingChains.Count == 1
            && result.BlockingChains[0].ChainLength == 3;
    }

    // Feature: analytics-reporting, Property 3: A cycle A→B→A is detected in circularDependencies
    [Property(MaxTest = 100)]
    public bool Cycle_IsDetected()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var stories = new[]
        {
            DependencyGenerator.CreateStory(a),
            DependencyGenerator.CreateStory(b)
        };

        var links = new[]
        {
            DependencyGenerator.CreateLink(a, b, "blocks"),
            DependencyGenerator.CreateLink(b, a, "blocks")
        };

        var result = (DependencyAnalysisResponse)_sut.Analyze(links, stories);

        return result.CircularDependencies.Count > 0
            && result.CircularDependencies.Any(cycle => cycle.Contains(a) && cycle.Contains(b));
    }

    // Feature: analytics-reporting, Property 4: Blocked stories: story with incoming "is_blocked_by" where blocker status ≠ Done appears in blockedStories
    [Property(MaxTest = 100)]
    public bool BlockedByActiveBlocker_AppearsInBlockedStories()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();

        var stories = new[]
        {
            DependencyGenerator.CreateStory(blockerId, "In Progress"),
            DependencyGenerator.CreateStory(blockedId, "Backlog")
        };

        // "is_blocked_by": source is blocked by target
        var links = new[]
        {
            DependencyGenerator.CreateLink(blockedId, blockerId, "is_blocked_by")
        };

        var result = (DependencyAnalysisResponse)_sut.Analyze(links, stories);

        return result.BlockedStories.Any(bs => bs.StoryId == blockedId);
    }

    // Feature: analytics-reporting, Property 5: Stories where all blockers are Done do NOT appear in blockedStories
    [Property(MaxTest = 100)]
    public bool AllBlockersDone_NotInBlockedStories()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();

        var stories = new[]
        {
            DependencyGenerator.CreateStory(blockerId, "Done"),
            DependencyGenerator.CreateStory(blockedId, "Backlog")
        };

        var links = new[]
        {
            DependencyGenerator.CreateLink(blockedId, blockerId, "is_blocked_by")
        };

        var result = (DependencyAnalysisResponse)_sut.Analyze(links, stories);

        return !result.BlockedStories.Any(bs => bs.StoryId == blockedId);
    }

    // Feature: analytics-reporting, Property 6: Chain count is always >= 0 and totalDependencies matches link count
    [Property(MaxTest = 100)]
    public bool ChainCount_NonNegative_And_TotalDependencies_MatchesLinkCount(ushort seed)
    {
        var rng = new Random(seed);
        var storyIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var stories = storyIds.Select(id => DependencyGenerator.CreateStory(id)).ToArray();

        var linkCount = rng.Next(0, 7);
        var links = Enumerable.Range(0, linkCount)
            .Select(_ =>
            {
                var src = storyIds[rng.Next(storyIds.Length)];
                var tgt = storyIds[rng.Next(storyIds.Length)];
                while (tgt == src) tgt = storyIds[rng.Next(storyIds.Length)];
                return DependencyGenerator.CreateLink(src, tgt, "blocks");
            })
            .ToList();

        var result = (DependencyAnalysisResponse)_sut.Analyze(links, stories);

        return result.BlockingChains.Count >= 0 && result.TotalDependencies == links.Count;
    }
}
