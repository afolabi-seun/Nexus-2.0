using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class RiskRegister : IOrganizationEntity
{
    public Guid RiskRegisterId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? SprintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = "Medium";
    public string Likelihood { get; set; } = "Medium";
    public string MitigationStatus { get; set; } = "Open";
    public Guid CreatedBy { get; set; }
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
