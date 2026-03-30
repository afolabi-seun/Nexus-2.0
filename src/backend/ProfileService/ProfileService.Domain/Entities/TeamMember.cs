using ProfileService.Domain.Common;

namespace ProfileService.Domain.Entities;

public class TeamMember : IOrganizationEntity
{
    public Guid TeamMemberId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid PrimaryDepartmentId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Title { get; set; }
    public string ProfessionalId { get; set; } = string.Empty;
    public string? Skills { get; set; }
    public string Availability { get; set; } = "Available";
    public int MaxConcurrentTasks { get; set; } = 5;
    public bool IsFirstTimeUser { get; set; } = true;
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public Organization Organization { get; set; } = null!;
    public Department PrimaryDepartment { get; set; } = null!;
    public ICollection<DepartmentMember> DepartmentMemberships { get; set; } = new List<DepartmentMember>();
}
