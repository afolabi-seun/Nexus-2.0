namespace WorkService.Application.DTOs.RiskRegisters;

public class CreateRiskRequest
{
    public Guid ProjectId { get; set; }
    public Guid? SprintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = "Medium";
    public string Likelihood { get; set; } = "Medium";
    public string MitigationStatus { get; set; } = "Open";
}
