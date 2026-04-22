using BillingService.Domain.Results;

namespace BillingService.Domain.Interfaces.Services.Usage;

public interface IUsageService
{
    Task<ServiceResult<object>> GetUsageAsync(Guid organizationId, CancellationToken ct);
    Task<ServiceResult<object>> IncrementAsync(Guid organizationId, string metricName, long value, CancellationToken ct);
}
