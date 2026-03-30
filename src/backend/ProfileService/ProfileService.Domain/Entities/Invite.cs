using ProfileService.Domain.Common;

namespace ProfileService.Domain.Entities;

public class Invite : IOrganizationEntity
{
    public Guid InviteId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid RoleId { get; set; }
    public Guid InvitedByMemberId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    // Navigation
    public Organization Organization { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public TeamMember InvitedByMember { get; set; } = null!;
}
