using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.StripeEvents;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories.StripeEvents;

public class StripeEventRepository : GenericRepository<StripeEvent>, IStripeEventRepository
{
    private readonly BillingDbContext _db;

    public StripeEventRepository(BillingDbContext context) : base(context)
    {
        _db = context;
    }

    public async Task<bool> ExistsAsync(string stripeEventId, CancellationToken ct) =>
        await _db.StripeEvents.AnyAsync(e => e.StripeEventId == stripeEventId, ct);
}
