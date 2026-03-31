namespace BillingService.Domain.Interfaces.Services.Subscriptions;

public interface ISubscriptionService
{
    Task<object> GetCurrentAsync(Guid organizationId, CancellationToken ct);
    Task<object> CreateAsync(Guid organizationId, object request, CancellationToken ct);
    Task<object> UpgradeAsync(Guid organizationId, object request, CancellationToken ct);
    Task<object> DowngradeAsync(Guid organizationId, object request, CancellationToken ct);
    Task<object> CancelAsync(Guid organizationId, CancellationToken ct);
}
