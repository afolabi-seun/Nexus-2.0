namespace UtilityService.Domain.Helpers;

public static class NotificationTypes
{
    public const string StoryAssigned = "StoryAssigned";
    public const string TaskAssigned = "TaskAssigned";
    public const string SprintStarted = "SprintStarted";
    public const string SprintEnded = "SprintEnded";
    public const string MentionedInComment = "MentionedInComment";
    public const string StoryStatusChanged = "StoryStatusChanged";
    public const string TaskStatusChanged = "TaskStatusChanged";
    public const string DueDateApproaching = "DueDateApproaching";

    public static readonly string[] All =
    {
        StoryAssigned, TaskAssigned, SprintStarted, SprintEnded,
        MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching
    };
}
