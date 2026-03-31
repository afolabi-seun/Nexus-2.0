using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class CostSnapshot : IOrganizationEntity
{
    public Guid CostSnapshotId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalBillableHours { get; set; }
    public decimal TotalNonBillableHours { get; set; }
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
