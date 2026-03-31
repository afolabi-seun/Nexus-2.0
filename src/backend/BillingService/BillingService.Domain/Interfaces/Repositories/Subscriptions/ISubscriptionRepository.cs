using BillingService.Domain.Entities;

namespace BillingService.Domain.Interfaces.Repositories.Subscriptions;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct);
    Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken ct);
    Task<Subscription> CreateAsync(Subscription subscription, CancellationToken ct);
    Task UpdateAsync(Subscription subscription, CancellationToken ct);
    Task<List<Subscription>> GetExpiredTrialsAsync(DateTime cutoff, CancellationToken ct);
    Task<List<Subscription>> GetSubscriptionsDueForDowngradeAsync(DateTime cutoff, CancellationToken ct);
    Task<List<Subscription>> GetAllWithPlansAsync(CancellationToken ct);
    Task<int> GetCountByStatusAsync(string status, CancellationToken ct);
}
