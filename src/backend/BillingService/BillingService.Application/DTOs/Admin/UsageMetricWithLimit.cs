namespace BillingService.Application.DTOs.Admin;

public record UsageMetricWithLimit(
    string MetricName,
    long CurrentValue,
    long Limit,
    double PercentUsed);
