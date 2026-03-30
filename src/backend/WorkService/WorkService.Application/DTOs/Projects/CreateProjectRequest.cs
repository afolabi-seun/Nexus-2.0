namespace WorkService.Application.DTOs.Projects;

public class CreateProjectRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LeadId { get; set; }
}
