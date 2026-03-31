namespace WorkService.Application.DTOs.CostRates;

public class UpdateCostRateRequest
{
    public decimal HourlyRate { get; set; }
    public DateTime? EffectiveFrom { get; set; }
}
