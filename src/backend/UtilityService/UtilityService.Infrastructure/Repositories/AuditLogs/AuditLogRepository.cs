using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Repositories.AuditLogs;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly UtilityDbContext _context;

    public AuditLogRepository(UtilityDbContext context) => _context = context;

    public async Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(ct);
        return auditLog;
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> QueryAsync(
        Guid organizationId, string? serviceName, string? action, string? entityType,
        string? userId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize,
        CancellationToken ct = default)
    {
        _context.OrganizationId = organizationId;
        var query = _context.AuditLogs.AsNoTracking();

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
