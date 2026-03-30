using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories;
using BillingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories.Subscriptions;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly BillingDbContext _context;

    public SubscriptionRepository(BillingDbContext context) => _context = context;

    public async Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct) =>
        await _context.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Include(s => s.ScheduledPlan)
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);

    public async Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken ct) =>
        await _context.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Include(s => s.ScheduledPlan)
            .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, ct);

    public async Task<Subscription> CreateAsync(Subscription subscription, CancellationToken ct)
    {
        await _context.Subscriptions.AddAsync(subscription, ct);
        await _context.SaveChangesAsync(ct);
        return subscription;
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken ct)
    {
        subscription.DateUpdated = DateTime.UtcNow;
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<Subscription>> GetExpiredTrialsAsync(DateTime cutoff, CancellationToken ct) =>
        await _context.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Trialing && s.TrialEndDate <= cutoff)
            .ToListAsync(ct);

    public async Task<List<Subscription>> GetSubscriptionsDueForDowngradeAsync(DateTime cutoff, CancellationToken ct) =>
        await _context.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Include(s => s.ScheduledPlan)
            .Where(s => s.ScheduledPlanId != null && s.CurrentPeriodEnd <= cutoff)
            .ToListAsync(ct);
}
