namespace ProfileService.Application.DTOs.TeamMembers;

public class TeamMemberResponse
{
    public Guid TeamMemberId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Title { get; set; }
    public string ProfessionalId { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string FlgStatus { get; set; } = string.Empty;
}
