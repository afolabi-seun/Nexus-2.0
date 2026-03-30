namespace ProfileService.Application.DTOs.Organizations;

public class UpdateOrganizationRequest
{
    public string? OrganizationName { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? TimeZone { get; set; }
    public int? DefaultSprintDurationWeeks { get; set; }
}
