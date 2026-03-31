namespace UtilityService.Domain.Interfaces.Services.Notifications;

public interface INotificationService
{
    Task DispatchAsync(object request, CancellationToken ct = default);
    Task<object> GetUserHistoryAsync(Guid userId, Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default);
    Task RetryFailedAsync(CancellationToken ct = default);
}
