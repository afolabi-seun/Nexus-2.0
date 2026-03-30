namespace ProfileService.Application.DTOs.Preferences;

public class ResolvedPreferencesResponse
{
    public string Theme { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string DefaultBoardView { get; set; } = string.Empty;
    public string DigestFrequency { get; set; } = string.Empty;
    public string NotificationChannels { get; set; } = string.Empty;
    public bool KeyboardShortcutsEnabled { get; set; }
    public string DateFormat { get; set; } = string.Empty;
    public string TimeFormat { get; set; } = string.Empty;
    public string StoryPointScale { get; set; } = string.Empty;
    public bool AutoAssignmentEnabled { get; set; }
    public string AutoAssignmentStrategy { get; set; } = string.Empty;
    public bool WipLimitsEnabled { get; set; }
    public int DefaultWipLimit { get; set; }
    public int AuditRetentionDays { get; set; }
    public int MaxConcurrentTasksDefault { get; set; }
}
