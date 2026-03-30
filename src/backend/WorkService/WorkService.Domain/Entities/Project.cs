using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class Project : IOrganizationEntity
{
    public Guid ProjectId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LeadId { get; set; }
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
