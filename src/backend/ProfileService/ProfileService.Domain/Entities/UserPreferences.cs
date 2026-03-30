using ProfileService.Domain.Common;

namespace ProfileService.Domain.Entities;

public class UserPreferences : IOrganizationEntity
{
    public Guid UserPreferencesId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid TeamMemberId { get; set; }
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "en";
    public string? TimezoneOverride { get; set; }
    public string? DefaultBoardView { get; set; }
    public string? DefaultBoardFilters { get; set; }
    public string? DashboardLayout { get; set; }
    public string? EmailDigestFrequency { get; set; }
    public bool KeyboardShortcutsEnabled { get; set; } = true;
    public string DateFormat { get; set; } = "ISO";
    public string TimeFormat { get; set; } = "H24";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public TeamMember TeamMember { get; set; } = null!;
}
