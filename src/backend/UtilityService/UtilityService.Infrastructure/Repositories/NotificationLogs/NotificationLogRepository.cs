using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Helpers;
using UtilityService.Domain.Interfaces.Repositories.NotificationLogs;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Repositories.Generics;

namespace UtilityService.Infrastructure.Repositories.NotificationLogs;

public class NotificationLogRepository : GenericRepository<NotificationLog>, INotificationLogRepository
{
    private readonly UtilityDbContext _db;

    public NotificationLogRepository(UtilityDbContext db) : base(db) => _db = db;

    public async Task<(IEnumerable<NotificationLog> Items, int TotalCount)> QueryByUserAsync(
        Guid userId, Guid organizationId, string? notificationType, string? channel,
        string? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize,
        CancellationToken ct = default)
    {
        _db.OrganizationId = organizationId;
        var query = _db.NotificationLogs.AsNoTracking()
            .Where(e => e.UserId == userId);

        if (!string.IsNullOrEmpty(notificationType))
            query = query.Where(e => e.NotificationType == notificationType);
        if (!string.IsNullOrEmpty(channel))
            query = query.Where(e => e.Channel == channel);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(e => e.DateCreated >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(e => e.DateCreated <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(e => e.DateCreated)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<NotificationLog>> GetFailedForRetryAsync(int maxRetryCount, CancellationToken ct = default)
        => await _db.NotificationLogs.IgnoreQueryFilters()
            .Where(e => e.Status == NotificationStatuses.Failed && e.RetryCount < maxRetryCount)
            .ToListAsync(ct);
}
