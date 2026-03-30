namespace ProfileService.Domain.Entities;

public class OrganizationSettings
{
    // Workflow
    public string StoryPointScale { get; set; } = "Fibonacci";
    public Dictionary<string, string[]> RequiredFieldsByStoryType { get; set; } = new();
    public bool AutoAssignmentEnabled { get; set; } = false;
    public string AutoAssignmentStrategy { get; set; } = "LeastLoaded";

    // General
    public string[] WorkingDays { get; set; } = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];
    public string WorkingHoursStart { get; set; } = "09:00";
    public string WorkingHoursEnd { get; set; } = "17:00";
    public string? PrimaryColor { get; set; }

    // Board
    public string DefaultBoardView { get; set; } = "Kanban";
    public bool WipLimitsEnabled { get; set; } = false;
    public int DefaultWipLimit { get; set; } = 0;

    // Notification
    public string DefaultNotificationChannels { get; set; } = "Email,Push,InApp";
    public string DigestFrequency { get; set; } = "Realtime";

    // Data
    public int AuditRetentionDays { get; set; } = 90;
}
