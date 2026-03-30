namespace ProfileService.Domain.Entities;

public class Organization
{
    public Guid OrganizationId { get; set; } = Guid.NewGuid();
    public string OrganizationName { get; set; } = string.Empty;
    public string StoryIdPrefix { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public int DefaultSprintDurationWeeks { get; set; } = 2;
    public string? SettingsJson { get; set; }
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}
