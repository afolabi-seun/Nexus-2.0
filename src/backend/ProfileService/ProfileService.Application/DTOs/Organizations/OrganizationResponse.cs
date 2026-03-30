namespace ProfileService.Application.DTOs.Organizations;

public class OrganizationResponse
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string StoryIdPrefix { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public int DefaultSprintDurationWeeks { get; set; }
    public OrganizationSettingsResponse? Settings { get; set; }
    public string FlgStatus { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
