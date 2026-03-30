using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories;
using BillingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories.Plans;

public class PlanRepository : IPlanRepository
{
    private readonly BillingDbContext _context;

    public PlanRepository(BillingDbContext context) => _context = context;

    public async Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct) =>
        await _context.Plans.FirstOrDefaultAsync(p => p.PlanId == planId, ct);

    public async Task<Plan?> GetByCodeAsync(string planCode, CancellationToken ct) =>
        await _context.Plans.FirstOrDefaultAsync(p => p.PlanCode == planCode, ct);

    public async Task<List<Plan>> GetAllActiveAsync(CancellationToken ct) =>
        await _context.Plans.Where(p => p.IsActive).OrderBy(p => p.TierLevel).ToListAsync(ct);

    public async Task CreateAsync(Plan plan, CancellationToken ct)
    {
        await _context.Plans.AddAsync(plan, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsByCodeAsync(string planCode, CancellationToken ct) =>
        await _context.Plans.AnyAsync(p => p.PlanCode == planCode, ct);
}
