namespace ProfileService.Application.DTOs.Preferences;

public class UserPreferencesResponse
{
    public string Theme { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? TimezoneOverride { get; set; }
    public string? DefaultBoardView { get; set; }
    public object? DefaultBoardFilters { get; set; }
    public object? DashboardLayout { get; set; }
    public string? EmailDigestFrequency { get; set; }
    public bool KeyboardShortcutsEnabled { get; set; }
    public string DateFormat { get; set; } = string.Empty;
    public string TimeFormat { get; set; } = string.Empty;
}
