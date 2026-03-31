using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.StripeEvents;
using BillingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories.StripeEvents;

public class StripeEventRepository : IStripeEventRepository
{
    private readonly BillingDbContext _context;

    public StripeEventRepository(BillingDbContext context) => _context = context;

    public async Task<bool> ExistsAsync(string stripeEventId, CancellationToken ct) =>
        await _context.StripeEvents.AnyAsync(e => e.StripeEventId == stripeEventId, ct);

    public async Task CreateAsync(StripeEvent stripeEvent, CancellationToken ct)
    {
        await _context.StripeEvents.AddAsync(stripeEvent, ct);
        await _context.SaveChangesAsync(ct);
    }
}
