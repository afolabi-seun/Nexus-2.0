using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.ActivityLogs;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;

namespace WorkService.Infrastructure.Repositories.ActivityLogs;

public class ActivityLogRepository : GenericRepository<ActivityLog>, IActivityLogRepository
{
    private readonly WorkDbContext _db;

    public ActivityLogRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ActivityLog>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await _db.ActivityLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.DateCreated)
            .ToListAsync(ct);

    public async Task<(IEnumerable<ActivityLog> Items, int TotalCount)> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ActivityLogs
            .Where(a => a.OrganizationId == organizationId)
            .OrderByDescending(a => a.DateCreated);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}
