using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.ArchivedAuditLogs;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Repositories.ArchivedAuditLogs;

public class ArchivedAuditLogRepository : IArchivedAuditLogRepository
{
    private readonly UtilityDbContext _context;

    public ArchivedAuditLogRepository(UtilityDbContext context) => _context = context;

    public async Task AddRangeAsync(IEnumerable<ArchivedAuditLog> logs, CancellationToken ct = default)
    {
        _context.ArchivedAuditLogs.AddRange(logs);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<ArchivedAuditLog> Items, int TotalCount)> QueryAsync(
        Guid organizationId, string? serviceName, string? action, string? entityType,
        string? userId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.ArchivedAuditLogs.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(serviceName))
            query = query.Where(e => e.ServiceName == serviceName);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(e => e.Action == action);
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(e => e.EntityType == entityType);
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(e => e.UserId == userId);
        if (dateFrom.HasValue)
            query = query.Where(e => e.DateCreated >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(e => e.DateCreated <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(e => e.DateCreated)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }
}
