namespace WorkService.Application.DTOs.CostSnapshots;

public class CostSnapshotResponse
{
    public Guid CostSnapshotId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalBillableHours { get; set; }
    public decimal TotalNonBillableHours { get; set; }
    public DateTime SnapshotDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DateCreated { get; set; }
}
