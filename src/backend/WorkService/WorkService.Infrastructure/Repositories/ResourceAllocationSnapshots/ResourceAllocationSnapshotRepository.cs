using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.ResourceAllocationSnapshots;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Repositories.ResourceAllocationSnapshots;

public class ResourceAllocationSnapshotRepository : IResourceAllocationSnapshotRepository
{
    private readonly WorkDbContext _db;

    public ResourceAllocationSnapshotRepository(WorkDbContext db) => _db = db;

    public async Task<ResourceAllocationSnapshot> AddOrUpdateAsync(
        ResourceAllocationSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await _db.ResourceAllocationSnapshots.FirstOrDefaultAsync(s =>
            s.ProjectId == snapshot.ProjectId
            && s.MemberId == snapshot.MemberId
            && s.PeriodStart == snapshot.PeriodStart
            && s.PeriodEnd == snapshot.PeriodEnd, ct);

        if (existing is not null)
        {
            existing.TotalLoggedHours = snapshot.TotalLoggedHours;
            existing.ExpectedHours = snapshot.ExpectedHours;
            existing.UtilizationPercentage = snapshot.UtilizationPercentage;
            existing.BillableHours = snapshot.BillableHours;
            existing.NonBillableHours = snapshot.NonBillableHours;
            existing.OvertimeHours = snapshot.OvertimeHours;
            existing.SnapshotDate = snapshot.SnapshotDate;
            _db.ResourceAllocationSnapshots.Update(existing);
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        _db.ResourceAllocationSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task<IEnumerable<ResourceAllocationSnapshot>> GetByProjectAsync(
        Guid projectId, DateTime periodStart, DateTime periodEnd,
        CancellationToken ct = default)
    {
        return await _db.ResourceAllocationSnapshots
            .Where(s => s.ProjectId == projectId
                        && s.PeriodStart == periodStart
                        && s.PeriodEnd == periodEnd)
            .ToListAsync(ct);
    }
}
