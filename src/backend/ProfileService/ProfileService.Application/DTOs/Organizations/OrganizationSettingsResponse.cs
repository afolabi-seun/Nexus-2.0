namespace ProfileService.Application.DTOs.Organizations;

public class OrganizationSettingsResponse
{
    public string? StoryPointScale { get; set; }
    public Dictionary<string, string[]>? RequiredFieldsByStoryType { get; set; }
    public bool AutoAssignmentEnabled { get; set; }
    public string? AutoAssignmentStrategy { get; set; }
    public string[]? WorkingDays { get; set; }
    public string? WorkingHoursStart { get; set; }
    public string? WorkingHoursEnd { get; set; }
    public string? PrimaryColor { get; set; }
    public string? DefaultBoardView { get; set; }
    public bool WipLimitsEnabled { get; set; }
    public int DefaultWipLimit { get; set; }
    public string? DefaultNotificationChannels { get; set; }
    public string? DigestFrequency { get; set; }
    public int AuditRetentionDays { get; set; }
}
