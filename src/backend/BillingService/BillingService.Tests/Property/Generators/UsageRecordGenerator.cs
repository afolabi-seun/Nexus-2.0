using BillingService.Domain.Entities;
using BillingService.Domain.Enums;

namespace BillingService.Tests.Property.Generators;

public static class UsageRecordGenerator
{
    public static UsageRecord Create(Guid? orgId = null, string? metricName = null, long value = 10) => new()
    {
        UsageRecordId = Guid.NewGuid(),
        OrganizationId = orgId ?? Guid.NewGuid(),
        MetricName = metricName ?? MetricName.ActiveMembers,
        MetricValue = value,
        PeriodStart = DateTime.UtcNow.AddDays(-30),
        PeriodEnd = DateTime.UtcNow,
        DateUpdated = DateTime.UtcNow
    };
}
