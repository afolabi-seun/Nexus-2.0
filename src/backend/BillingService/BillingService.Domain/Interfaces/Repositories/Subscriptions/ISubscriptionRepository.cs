using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.Generics;

namespace BillingService.Domain.Interfaces.Repositories.Subscriptions;

public interface ISubscriptionRepository : IGenericRepository<Subscription>
{
    Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct);
    Task<List<Subscription>> GetExpiredTrialsAsync(DateTime cutoff, CancellationToken ct);
    Task<List<Subscription>> GetSubscriptionsDueForDowngradeAsync(DateTime cutoff, CancellationToken ct);
    Task<List<Subscription>> GetAllWithPlansAsync(CancellationToken ct);
    Task<int> GetCountByStatusAsync(string status, CancellationToken ct);
}
