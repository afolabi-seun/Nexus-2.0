namespace WorkService.Application.DTOs.Projects;

public class ProjectDetailResponse
{
    public Guid ProjectId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int StoryCount { get; set; }
    public int SprintCount { get; set; }
    public string FlgStatus { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
