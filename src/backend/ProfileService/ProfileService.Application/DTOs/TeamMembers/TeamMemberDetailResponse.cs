namespace ProfileService.Application.DTOs.TeamMembers;

public class TeamMemberDetailResponse : TeamMemberResponse
{
    public string[]? Skills { get; set; }
    public int MaxConcurrentTasks { get; set; }
    public int ActiveTaskCount { get; set; }
    public List<DepartmentMembershipResponse> DepartmentMemberships { get; set; } = new();
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
