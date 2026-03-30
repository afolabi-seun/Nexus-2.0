namespace ProfileService.Domain.Entities;

public class Role
{
    public Guid RoleId { get; set; } = Guid.NewGuid();
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PermissionLevel { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
