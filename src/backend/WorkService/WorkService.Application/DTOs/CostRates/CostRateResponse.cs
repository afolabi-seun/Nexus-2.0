namespace WorkService.Application.DTOs.CostRates;

public class CostRateResponse
{
    public Guid CostRateId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RateType { get; set; } = string.Empty;
    public Guid? MemberId { get; set; }
    public string? RoleName { get; set; }
    public Guid? DepartmentId { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string FlgStatus { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
