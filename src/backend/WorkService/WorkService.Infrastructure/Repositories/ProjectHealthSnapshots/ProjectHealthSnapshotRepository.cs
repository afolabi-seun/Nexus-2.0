using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.ProjectHealthSnapshots;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;

namespace WorkService.Infrastructure.Repositories.ProjectHealthSnapshots;

public class ProjectHealthSnapshotRepository : GenericRepository<ProjectHealthSnapshot>, IProjectHealthSnapshotRepository
{
    private readonly WorkDbContext _db;

    public ProjectHealthSnapshotRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<ProjectHealthSnapshot?> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _db.ProjectHealthSnapshots
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<ProjectHealthSnapshot>> GetHistoryByProjectAsync(
        Guid projectId, int count, CancellationToken ct = default)
    {
        return await _db.ProjectHealthSnapshots
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.SnapshotDate)
            .Take(count)
            .ToListAsync(ct);
    }
}
