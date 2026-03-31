using BillingService.Domain.Entities;

namespace BillingService.Domain.Interfaces.Repositories;

public interface IStripeEventRepository
{
    Task<bool> ExistsAsync(string stripeEventId, CancellationToken ct);
    Task CreateAsync(StripeEvent stripeEvent, CancellationToken ct);
}
