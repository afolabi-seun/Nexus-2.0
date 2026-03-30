namespace ProfileService.Domain.Helpers;

public static class SystemDefaults
{
    public const string Theme = "System";
    public const string Language = "en";
    public const string Timezone = "UTC";
    public const string DefaultBoardView = "Kanban";
    public const string DigestFrequency = "Realtime";
    public const string NotificationChannels = "Email,Push,InApp";
    public const bool KeyboardShortcutsEnabled = true;
    public const string DateFormat = "ISO";
    public const string TimeFormat = "H24";
    public const string StoryPointScale = "Fibonacci";
    public const bool AutoAssignmentEnabled = false;
    public const string AutoAssignmentStrategy = "LeastLoaded";
    public const bool WipLimitsEnabled = false;
    public const int DefaultWipLimit = 0;
    public const int AuditRetentionDays = 90;
    public const int MaxConcurrentTasksDefault = 5;
}
