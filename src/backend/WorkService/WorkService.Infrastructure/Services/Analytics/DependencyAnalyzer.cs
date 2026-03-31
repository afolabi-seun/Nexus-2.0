using WorkService.Application.DTOs.Analytics;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Services.Analytics;

namespace WorkService.Infrastructure.Services.Analytics;

public class DependencyAnalyzer : IDependencyAnalyzer
{
    private enum Color { White, Gray, Black }

    public object Analyze(
        IEnumerable<StoryLink> links,
        IEnumerable<Story> stories,
        Guid? filterSprintId = null)
    {
        var storyList = stories.ToList();
        var linkList = links.ToList();

        // Filter stories by sprint if specified
        var storyIds = new HashSet<Guid>(storyList.Select(s => s.StoryId));
        var storyMap = storyList.ToDictionary(s => s.StoryId);

        // Build adjacency list: blocker → blocked (source blocks target)
        var adjacency = new Dictionary<Guid, List<Guid>>();
        var incomingEdges = new Dictionary<Guid, List<Guid>>();
        var relevantLinks = new List<StoryLink>();

        foreach (var link in linkList)
        {
            Guid from, to;
            if (link.LinkType == "blocks")
            {
                from = link.SourceStoryId;
                to = link.TargetStoryId;
            }
            else if (link.LinkType == "is_blocked_by")
            {
                from = link.TargetStoryId;
                to = link.SourceStoryId;
            }
            else
            {
                continue;
            }

            // Only include edges where both stories are in scope
            if (!storyIds.Contains(from) || !storyIds.Contains(to))
                continue;

            relevantLinks.Add(link);

            if (!adjacency.ContainsKey(from))
                adjacency[from] = new List<Guid>();
            adjacency[from].Add(to);

            if (!incomingEdges.ContainsKey(to))
                incomingEdges[to] = new List<Guid>();
            incomingEdges[to].Add(from);
        }

        // Collect all nodes that participate in dependency edges
        var allNodes = new HashSet<Guid>();
        foreach (var kvp in adjacency)
        {
            allNodes.Add(kvp.Key);
            foreach (var target in kvp.Value)
                allNodes.Add(target);
        }

        // Detect circular dependencies using DFS coloring
        var circularDependencies = DetectCycles(allNodes, adjacency);

        // Find root nodes (no incoming edges among dependency nodes)
        var roots = allNodes.Where(n => !incomingEdges.ContainsKey(n) || incomingEdges[n].Count == 0).ToList();

        // Find all chains via DFS from roots
        var chains = new List<DependencyChain>();
        var visited = new HashSet<Guid>();
        // Nodes in cycles should not be traversed for chain detection
        var cycleNodes = new HashSet<Guid>(circularDependencies.SelectMany(c => c));

        foreach (var root in roots)
        {
            FindChains(root, new List<Guid>(), adjacency, storyMap, chains, visited, cycleNodes, filterSprintId);
        }

        // Find blocked stories: stories with incoming is_blocked_by where blocker status != Done
        var blockedStories = new List<BlockedStoryDetail>();
        foreach (var kvp in incomingEdges)
        {
            var storyId = kvp.Key;
            if (!storyMap.TryGetValue(storyId, out var story))
                continue;

            var activeBlockers = kvp.Value
                .Where(blockerId => storyMap.TryGetValue(blockerId, out var blocker) && blocker.Status != "Done")
                .ToList();

            if (activeBlockers.Count > 0)
            {
                blockedStories.Add(new BlockedStoryDetail
                {
                    StoryId = story.StoryId,
                    StoryKey = story.StoryKey,
                    Title = story.Title,
                    Status = story.Status,
                    BlockedByStoryIds = activeBlockers
                });
            }
        }

        return new DependencyAnalysisResponse
        {
            TotalDependencies = relevantLinks.Count,
            BlockingChains = chains,
            BlockedStories = blockedStories,
            CircularDependencies = circularDependencies
        };
    }

    private static void FindChains(
        Guid current,
        List<Guid> currentPath,
        Dictionary<Guid, List<Guid>> adjacency,
        Dictionary<Guid, Story> storyMap,
        List<DependencyChain> chains,
        HashSet<Guid> visited,
        HashSet<Guid> cycleNodes,
        Guid? filterSprintId)
    {
        currentPath.Add(current);

        var hasChildren = adjacency.TryGetValue(current, out var children) && children.Count > 0;
        var validChildren = hasChildren
            ? children!.Where(c => !cycleNodes.Contains(c) && !currentPath.Contains(c)).ToList()
            : new List<Guid>();

        if (validChildren.Count == 0)
        {
            // Leaf node — record chain if length > 1
            if (currentPath.Count > 1)
            {
                var chainStories = currentPath
                    .Where(id => storyMap.ContainsKey(id))
                    .Select(id =>
                    {
                        var s = storyMap[id];
                        return new ChainStoryDetail
                        {
                            StoryId = s.StoryId,
                            StoryKey = s.StoryKey,
                            Title = s.Title,
                            Status = s.Status,
                            AssigneeId = s.AssigneeId
                        };
                    })
                    .ToList();

                var criticalPath = currentPath.Any(id =>
                    storyMap.TryGetValue(id, out var s) && s.SprintId != null &&
                    (filterSprintId == null || s.SprintId == filterSprintId));

                chains.Add(new DependencyChain
                {
                    ChainLength = chainStories.Count,
                    Stories = chainStories,
                    CriticalPath = criticalPath
                });
            }
        }
        else
        {
            foreach (var child in validChildren)
            {
                FindChains(child, new List<Guid>(currentPath), adjacency, storyMap, chains, visited, cycleNodes, filterSprintId);
            }
        }
    }

    private static List<List<Guid>> DetectCycles(
        HashSet<Guid> allNodes,
        Dictionary<Guid, List<Guid>> adjacency)
    {
        var color = new Dictionary<Guid, Color>();
        foreach (var node in allNodes)
            color[node] = Color.White;

        var cycles = new List<List<Guid>>();
        var path = new List<Guid>();

        foreach (var node in allNodes)
        {
            if (color[node] == Color.White)
            {
                DfsCycleDetect(node, adjacency, color, path, cycles);
            }
        }

        return cycles;
    }

    private static void DfsCycleDetect(
        Guid node,
        Dictionary<Guid, List<Guid>> adjacency,
        Dictionary<Guid, Color> color,
        List<Guid> path,
        List<List<Guid>> cycles)
    {
        color[node] = Color.Gray;
        path.Add(node);

        if (adjacency.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!color.ContainsKey(neighbor))
                    continue;

                if (color[neighbor] == Color.Gray)
                {
                    // Found a cycle — extract the cycle from path
                    var cycleStart = path.IndexOf(neighbor);
                    if (cycleStart >= 0)
                    {
                        var cycle = path.Skip(cycleStart).ToList();
                        cycles.Add(cycle);
                    }
                }
                else if (color[neighbor] == Color.White)
                {
                    DfsCycleDetect(neighbor, adjacency, color, path, cycles);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        color[node] = Color.Black;
    }
}
