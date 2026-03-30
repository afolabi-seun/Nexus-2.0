namespace BillingService.Application.DTOs.Usage;

public record IncrementUsageRequest(string MetricName, long Value);
