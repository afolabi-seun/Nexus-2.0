namespace ProfileService.Application.DTOs.Preferences;

public class UserPreferencesRequest
{
    public string? Theme { get; set; }
    public string? Language { get; set; }
    public string? TimezoneOverride { get; set; }
    public string? DefaultBoardView { get; set; }
    public object? DefaultBoardFilters { get; set; }
    public object? DashboardLayout { get; set; }
    public string? EmailDigestFrequency { get; set; }
    public bool? KeyboardShortcutsEnabled { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
}
