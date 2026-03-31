using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.VelocitySnapshots;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Repositories.VelocitySnapshots;

public class VelocitySnapshotRepository : IVelocitySnapshotRepository
{
    private readonly WorkDbContext _db;

    public VelocitySnapshotRepository(WorkDbContext db) => _db = db;

    public async Task<VelocitySnapshot> AddOrUpdateAsync(VelocitySnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await _db.VelocitySnapshots.FirstOrDefaultAsync(s =>
            s.ProjectId == snapshot.ProjectId
            && s.SprintId == snapshot.SprintId, ct);

        if (existing is not null)
        {
            existing.SprintName = snapshot.SprintName;
            existing.StartDate = snapshot.StartDate;
            existing.EndDate = snapshot.EndDate;
            existing.CommittedPoints = snapshot.CommittedPoints;
            existing.CompletedPoints = snapshot.CompletedPoints;
            existing.TotalLoggedHours = snapshot.TotalLoggedHours;
            existing.AverageHoursPerPoint = snapshot.AverageHoursPerPoint;
            existing.CompletedStoryCount = snapshot.CompletedStoryCount;
            existing.SnapshotDate = snapshot.SnapshotDate;
            _db.VelocitySnapshots.Update(existing);
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        _db.VelocitySnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task<IEnumerable<VelocitySnapshot>> GetByProjectAsync(
        Guid projectId, int count, CancellationToken ct = default)
    {
        return await _db.VelocitySnapshots
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.EndDate)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<VelocitySnapshot?> GetBySprintAsync(Guid sprintId, CancellationToken ct = default)
    {
        return await _db.VelocitySnapshots
            .FirstOrDefaultAsync(s => s.SprintId == sprintId, ct);
    }
}
