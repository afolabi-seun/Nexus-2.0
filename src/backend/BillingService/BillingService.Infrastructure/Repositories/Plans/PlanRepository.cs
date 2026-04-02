using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Repositories.Generics;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Infrastructure.Repositories.Plans;

public class PlanRepository : GenericRepository<Plan>, IPlanRepository
{
    private readonly BillingDbContext _db;

    public PlanRepository(BillingDbContext context) : base(context)
    {
        _db = context;
    }

    public async Task<Plan?> GetByCodeAsync(string planCode, CancellationToken ct) =>
        await _db.Plans.FirstOrDefaultAsync(p => p.PlanCode == planCode, ct);

    public async Task<List<Plan>> GetAllActiveAsync(CancellationToken ct) =>
        await _db.Plans.Where(p => p.IsActive).OrderBy(p => p.TierLevel).ToListAsync(ct);

    public async Task<bool> ExistsByCodeAsync(string planCode, CancellationToken ct) =>
        await _db.Plans.AnyAsync(p => p.PlanCode == planCode, ct);
}
