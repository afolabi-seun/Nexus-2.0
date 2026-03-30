namespace ProfileService.Application.DTOs.TeamMembers;

public class TeamMemberInternalResponse
{
    public Guid TeamMemberId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FlgStatus { get; set; } = string.Empty;
    public bool IsFirstTimeUser { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PrimaryDepartmentId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}
