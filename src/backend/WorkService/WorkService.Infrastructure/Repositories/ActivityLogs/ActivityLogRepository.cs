using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Repositories.ActivityLogs;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly WorkDbContext _db;

    public ActivityLogRepository(WorkDbContext db) => _db = db;

    public async Task<ActivityLog> AddAsync(ActivityLog log, CancellationToken ct = default)
    {
        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync(ct);
        return log;
    }

    public async Task<IEnumerable<ActivityLog>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await _db.ActivityLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.DateCreated)
            .ToListAsync(ct);
}
