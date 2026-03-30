namespace ProfileService.Application.DTOs.TeamMembers;

public class AddDepartmentRequest
{
    public Guid DepartmentId { get; set; }
    public Guid RoleId { get; set; }
}
