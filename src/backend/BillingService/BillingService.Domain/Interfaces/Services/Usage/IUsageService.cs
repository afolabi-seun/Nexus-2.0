namespace BillingService.Domain.Interfaces.Services;

public interface IUsageService
{
    Task<object> GetUsageAsync(Guid organizationId, CancellationToken ct);
    Task IncrementAsync(Guid organizationId, string metricName, long value, CancellationToken ct);
}
