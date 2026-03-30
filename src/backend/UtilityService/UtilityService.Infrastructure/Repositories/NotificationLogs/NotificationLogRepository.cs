using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Helpers;
using UtilityService.Domain.Interfaces.Repositories;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Repositories.NotificationLogs;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly UtilityDbContext _context;

    public NotificationLogRepository(UtilityDbContext context) => _context = context;

    public async Task<NotificationLog> AddAsync(NotificationLog log, CancellationToken ct = default)
    {
        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync(ct);
        return log;
    }

    public async Task UpdateAsync(NotificationLog log, CancellationToken ct = default)
    {
        _context.NotificationLogs.Update(log);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<NotificationLog> Items, int TotalCount)> QueryByUserAsync(
        Guid userId, Guid organizationId, string? notificationType, string? channel,
        string? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize,
        CancellationToken ct = default)
    {
        _context.OrganizationId = organizationId;
        var query = _context.NotificationLogs.AsNoTracking()
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
        => await _context.NotificationLogs.IgnoreQueryFilters()
            .Where(e => e.Status == NotificationStatuses.Failed && e.RetryCount < maxRetryCount)
            .ToListAsync(ct);
}
