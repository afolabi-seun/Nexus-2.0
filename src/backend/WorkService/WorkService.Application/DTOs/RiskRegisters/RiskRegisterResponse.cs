namespace WorkService.Application.DTOs.RiskRegisters;

public class RiskRegisterResponse
{
    public Guid RiskRegisterId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? SprintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Likelihood { get; set; } = string.Empty;
    public string MitigationStatus { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public string FlgStatus { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
