namespace ProfileService.Application.DTOs.Invites;

public class CreateInviteRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid RoleId { get; set; }
}
