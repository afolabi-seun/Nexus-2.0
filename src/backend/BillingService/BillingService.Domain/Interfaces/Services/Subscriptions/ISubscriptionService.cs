using BillingService.Domain.Results;

namespace BillingService.Domain.Interfaces.Services.Subscriptions;

public interface ISubscriptionService
{
    Task<ServiceResult<object>> GetCurrentAsync(Guid organizationId, CancellationToken ct);
    Task<ServiceResult<object>> CreateAsync(Guid organizationId, object request, CancellationToken ct);
    Task<ServiceResult<object>> UpgradeAsync(Guid organizationId, object request, CancellationToken ct);
    Task<ServiceResult<object>> DowngradeAsync(Guid organizationId, object request, CancellationToken ct);
    Task<ServiceResult<object>> CancelAsync(Guid organizationId, CancellationToken ct);
}
