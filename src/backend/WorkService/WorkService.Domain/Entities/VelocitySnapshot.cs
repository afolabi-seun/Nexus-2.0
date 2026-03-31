using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class VelocitySnapshot : IOrganizationEntity
{
    public Guid VelocitySnapshotId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid SprintId { get; set; }
    public string SprintName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CommittedPoints { get; set; }
    public int CompletedPoints { get; set; }
    public decimal TotalLoggedHours { get; set; }
    public decimal? AverageHoursPerPoint { get; set; }
    public int CompletedStoryCount { get; set; }
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
