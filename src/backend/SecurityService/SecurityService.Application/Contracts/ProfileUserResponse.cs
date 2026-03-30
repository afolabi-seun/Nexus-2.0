namespace SecurityService.Application.Contracts;

public class ProfileUserResponse
{
    public Guid TeamMemberId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FlgStatus { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Guid PrimaryDepartmentId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? DepartmentRole { get; set; }
    public bool IsFirstTimeUser { get; set; }
    public string? DeviceId { get; set; }
}
