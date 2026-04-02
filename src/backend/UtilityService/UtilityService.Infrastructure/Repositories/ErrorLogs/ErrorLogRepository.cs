using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.ErrorLogs;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Repositories.Generics;

namespace UtilityService.Infrastructure.Repositories.ErrorLogs;

public class ErrorLogRepository : GenericRepository<ErrorLog>, IErrorLogRepository
{
    private readonly UtilityDbContext _db;

    public ErrorLogRepository(UtilityDbContext db) : base(db) => _db = db;

    public async Task<(IEnumerable<ErrorLog> Items, int TotalCount)> QueryAsync(
        Guid organizationId, string? serviceName, string? errorCode, string? severity,
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize,
        CancellationToken ct = default)
    {
        _db.OrganizationId = organizationId;
        var query = _db.ErrorLogs.AsNoTracking();

        if (!string.IsNullOrEmpty(serviceName))
            query = query.Where(e => e.ServiceName == serviceName);
        if (!string.IsNullOrEmpty(errorCode))
            query = query.Where(e => e.ErrorCode == errorCode);
        if (!string.IsNullOrEmpty(severity))
            query = query.Where(e => e.Severity == severity);
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
