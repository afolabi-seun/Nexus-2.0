using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories;

public interface INotificationLogRepository
{
    Task<NotificationLog> AddAsync(NotificationLog log, CancellationToken ct = default);
    Task UpdateAsync(NotificationLog log, CancellationToken ct = default);
    Task<(IEnumerable<NotificationLog> Items, int TotalCount)> QueryByUserAsync(Guid userId, Guid organizationId, string? notificationType, string? channel, string? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<NotificationLog>> GetFailedForRetryAsync(int maxRetryCount, CancellationToken ct = default);
}
