namespace BillingService.Application.DTOs.Usage;

public record UsageMetric(string MetricName, long CurrentValue, int Limit, double PercentUsed);
