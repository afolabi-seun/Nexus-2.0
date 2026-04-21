using UtilityService.Domain.Results;

namespace UtilityService.Domain.Interfaces.Services.Notifications;

public interface INotificationService
{
    Task<ServiceResult<object>> DispatchAsync(object request, CancellationToken ct = default);
    Task<ServiceResult<object>> GetUserHistoryAsync(Guid userId, Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<object>> RetryFailedAsync(CancellationToken ct = default);
}
