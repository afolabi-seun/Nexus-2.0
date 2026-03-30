namespace ProfileService.Application.DTOs.Roles;

public class RoleResponse
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PermissionLevel { get; set; }
    public bool IsSystemRole { get; set; }
}
