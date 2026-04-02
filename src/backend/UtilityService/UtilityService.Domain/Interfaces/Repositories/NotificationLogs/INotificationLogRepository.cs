using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.Generics;

namespace UtilityService.Domain.Interfaces.Repositories.NotificationLogs;

public interface INotificationLogRepository : IGenericRepository<NotificationLog>
{
    Task<(IEnumerable<NotificationLog> Items, int TotalCount)> QueryByUserAsync(Guid userId, Guid organizationId, string? notificationType, string? channel, string? status, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<NotificationLog>> GetFailedForRetryAsync(int maxRetryCount, CancellationToken ct = default);
}
