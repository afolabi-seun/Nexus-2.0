using ProfileService.Domain.Common;

namespace ProfileService.Domain.Entities;

public class Department : IOrganizationEntity
{
    public Guid DepartmentId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public string? PreferencesJson { get; set; }
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public Organization Organization { get; set; } = null!;
    public ICollection<DepartmentMember> DepartmentMembers { get; set; } = new List<DepartmentMember>();
}
