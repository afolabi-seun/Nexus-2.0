using BillingService.Domain.Entities;
using BillingService.Domain.Enums;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories.Subscriptions;

public class SubscriptionRepository : GenericRepository<Subscription>, ISubscriptionRepository
{
    private readonly BillingDbContext _db;

    public SubscriptionRepository(BillingDbContext context) : base(context)
    {
        _db = context;
    }

    public async Task<Subscription?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken ct) =>
        await _db.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Include(s => s.ScheduledPlan)
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId, ct);

    public async Task<List<Subscription>> GetExpiredTrialsAsync(DateTime cutoff, CancellationToken ct) =>
        await _db.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Trialing && s.TrialEndDate <= cutoff)
            .ToListAsync(ct);

    public async Task<List<Subscription>> GetSubscriptionsDueForDowngradeAsync(DateTime cutoff, CancellationToken ct) =>
        await _db.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .Include(s => s.ScheduledPlan)
            .Where(s => s.ScheduledPlanId != null && s.CurrentPeriodEnd <= cutoff)
            .ToListAsync(ct);

    public async Task<List<Subscription>> GetAllWithPlansAsync(CancellationToken ct) =>
        await _db.Subscriptions
            .IgnoreQueryFilters()
            .Include(s => s.Plan)
            .ToListAsync(ct);

    public async Task<int> GetCountByStatusAsync(string status, CancellationToken ct) =>
        await _db.Subscriptions
            .IgnoreQueryFilters()
            .CountAsync(s => s.Status == status, ct);
}
