namespace ProfileService.Application.DTOs.TeamMembers;

public class DepartmentMembershipResponse
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime DateJoined { get; set; }
}
