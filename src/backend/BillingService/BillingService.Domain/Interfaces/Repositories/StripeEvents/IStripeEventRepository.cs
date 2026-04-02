using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.Generics;

namespace BillingService.Domain.Interfaces.Repositories.StripeEvents;

public interface IStripeEventRepository : IGenericRepository<StripeEvent>
{
    Task<bool> ExistsAsync(string stripeEventId, CancellationToken ct);
}
