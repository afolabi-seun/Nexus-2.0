namespace ProfileService.Application.DTOs.Organizations;

public class CreateOrganizationRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public string StoryIdPrefix { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public int DefaultSprintDurationWeeks { get; set; } = 2;
}
