namespace WorkService.Application.DTOs.Projects;

public class UpdateProjectRequest
{
    public string? ProjectName { get; set; }
    public string? ProjectKey { get; set; }
    public string? Description { get; set; }
    public Guid? LeadId { get; set; }
}
