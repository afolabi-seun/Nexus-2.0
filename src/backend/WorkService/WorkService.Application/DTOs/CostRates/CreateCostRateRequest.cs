namespace WorkService.Application.DTOs.CostRates;

public class CreateCostRateRequest
{
    public string RateType { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public Guid? MemberId { get; set; }
    public string? RoleName { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime? EffectiveFrom { get; set; }
}
