namespace ProfileService.Application.DTOs.TeamMembers;

public class UpdateTeamMemberRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Title { get; set; }
    public string[]? Skills { get; set; }
    public int? MaxConcurrentTasks { get; set; }
}
