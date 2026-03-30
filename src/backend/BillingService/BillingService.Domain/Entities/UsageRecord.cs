using BillingService.Domain.Common;

namespace BillingService.Domain.Entities;

public class UsageRecord : IOrganizationEntity
{
    public Guid UsageRecordId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public long MetricValue { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
