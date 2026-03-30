namespace WorkService.Domain.Helpers;

public static class WorkflowStateMachine
{
    private static readonly Dictionary<string, HashSet<string>> StoryTransitions = new()
    {
        ["Backlog"] = new() { "Ready" },
        ["Ready"] = new() { "InProgress" },
        ["InProgress"] = new() { "InReview" },
        ["InReview"] = new() { "InProgress", "QA" },
        ["QA"] = new() { "InProgress", "Done" },
        ["Done"] = new() { "Closed" },
        ["Closed"] = new()
    };

    private static readonly Dictionary<string, HashSet<string>> TaskTransitions = new()
    {
        ["ToDo"] = new() { "InProgress" },
        ["InProgress"] = new() { "InReview" },
        ["InReview"] = new() { "InProgress", "Done" },
        ["Done"] = new()
    };

    public static bool IsValidStoryTransition(string from, string to)
        => StoryTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsValidTaskTransition(string from, string to)
        => TaskTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyDictionary<string, HashSet<string>> GetStoryTransitions() => StoryTransitions;
    public static IReadOnlyDictionary<string, HashSet<string>> GetTaskTransitions() => TaskTransitions;
}
