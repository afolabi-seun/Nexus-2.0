namespace WorkService.Application.DTOs.RiskRegisters;

public class UpdateRiskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Severity { get; set; }
    public string? Likelihood { get; set; }
    public string? MitigationStatus { get; set; }
}
