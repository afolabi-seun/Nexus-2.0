using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class CostRate : IOrganizationEntity
{
    public Guid CostRateId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string RateType { get; set; } = string.Empty; // Member, RoleDepartment, OrgDefault
    public Guid? MemberId { get; set; }          // Set when RateType = Member
    public string? RoleName { get; set; }         // Set when RateType = RoleDepartment
    public Guid? DepartmentId { get; set; }       // Set when RateType = RoleDepartment
    public decimal HourlyRate { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public string FlgStatus { get; set; } = "A";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
