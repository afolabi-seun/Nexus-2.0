using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class ResourceAllocationSnapshot : IOrganizationEntity
{
    public Guid ResourceAllocationSnapshotId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid MemberId { get; set; }
    public decimal TotalLoggedHours { get; set; }
    public decimal ExpectedHours { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public decimal BillableHours { get; set; }
    public decimal NonBillableHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
