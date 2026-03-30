using ProfileService.Domain.Common;

namespace ProfileService.Domain.Entities;

public class DepartmentMember : IOrganizationEntity
{
    public Guid DepartmentMemberId { get; set; } = Guid.NewGuid();
    public Guid TeamMemberId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RoleId { get; set; }
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;

    // Navigation
    public TeamMember TeamMember { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
