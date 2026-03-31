using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class ProjectHealthSnapshot : IOrganizationEntity
{
    public Guid ProjectHealthSnapshotId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public decimal OverallScore { get; set; }
    public decimal VelocityScore { get; set; }
    public decimal BugRateScore { get; set; }
    public decimal OverdueScore { get; set; }
    public decimal RiskScore { get; set; }
    public string Trend { get; set; } = "stable";
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
