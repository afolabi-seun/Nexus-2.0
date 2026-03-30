namespace WorkService.Application.Contracts;

public class TeamMemberResponse
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public Guid? DepartmentId { get; set; }
    public int MaxConcurrentTasks { get; set; }
    public string Availability { get; set; } = string.Empty;
}
