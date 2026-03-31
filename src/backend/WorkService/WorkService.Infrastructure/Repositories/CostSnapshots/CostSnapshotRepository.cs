using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.CostSnapshots;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Repositories.CostSnapshots;

public class CostSnapshotRepository : ICostSnapshotRepository
{
    private readonly WorkDbContext _db;

    public CostSnapshotRepository(WorkDbContext db) => _db = db;

    public async Task<CostSnapshot> AddOrUpdateAsync(CostSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await _db.CostSnapshots.FirstOrDefaultAsync(s =>
            s.ProjectId == snapshot.ProjectId
            && s.PeriodStart == snapshot.PeriodStart
            && s.PeriodEnd == snapshot.PeriodEnd, ct);

        if (existing is not null)
        {
            existing.TotalCost = snapshot.TotalCost;
            existing.TotalBillableHours = snapshot.TotalBillableHours;
            existing.TotalNonBillableHours = snapshot.TotalNonBillableHours;
            existing.SnapshotDate = snapshot.SnapshotDate;
            _db.CostSnapshots.Update(existing);
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        _db.CostSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task<(IEnumerable<CostSnapshot> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.CostSnapshots.Where(s => s.ProjectId == projectId);

        if (dateFrom.HasValue)
            query = query.Where(s => s.PeriodEnd >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(s => s.PeriodStart <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.PeriodStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
