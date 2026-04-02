using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.CostRates;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.CostRates;

public class CostRateRepository : GenericRepository<CostRate>, ICostRateRepository
{
    private readonly WorkDbContext _db;

    public CostRateRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<CostRate> Items, int TotalCount)> ListAsync(
        Guid organizationId, string? rateType, Guid? memberId,
        Guid? departmentId, string? roleName, int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.CostRates.Where(r => r.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(rateType))
            query = query.Where(r => r.RateType == rateType);

        if (memberId.HasValue)
            query = query.Where(r => r.MemberId == memberId.Value);

        if (departmentId.HasValue)
            query = query.Where(r => r.DepartmentId == departmentId.Value);

        if (!string.IsNullOrEmpty(roleName))
            query = query.Where(r => r.RoleName == roleName);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.EffectiveFrom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<bool> ExistsDuplicateAsync(
        Guid organizationId, string rateType,
        Guid? memberId, string? roleName, Guid? departmentId,
        CancellationToken ct = default)
    {
        return await _db.CostRates.AnyAsync(r =>
            r.OrganizationId == organizationId
            && r.RateType == rateType
            && r.MemberId == memberId
            && r.RoleName == roleName
            && r.DepartmentId == departmentId, ct);
    }

    public async Task<IEnumerable<CostRate>> GetActiveRatesForMemberAsync(
        Guid organizationId, Guid memberId, DateTime asOfDate,
        CancellationToken ct = default)
    {
        return await _db.CostRates
            .Where(r => r.OrganizationId == organizationId
                        && r.RateType == "Member"
                        && r.MemberId == memberId
                        && r.EffectiveFrom <= asOfDate)
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<CostRate>> GetActiveRatesForRoleDepartmentAsync(
        Guid organizationId, string roleName, Guid departmentId, DateTime asOfDate,
        CancellationToken ct = default)
    {
        return await _db.CostRates
            .Where(r => r.OrganizationId == organizationId
                        && r.RateType == "RoleDepartment"
                        && r.RoleName == roleName
                        && r.DepartmentId == departmentId
                        && r.EffectiveFrom <= asOfDate)
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync(ct);
    }

    public async Task<CostRate?> GetOrgDefaultAsync(
        Guid organizationId, DateTime asOfDate,
        CancellationToken ct = default)
    {
        return await _db.CostRates
            .Where(r => r.OrganizationId == organizationId
                        && r.RateType == "OrgDefault"
                        && r.EffectiveFrom <= asOfDate)
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }
}
