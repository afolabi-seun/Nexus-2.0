using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.NotificationSettings;

public interface INotificationSettingService
{
    Task<ServiceResult<object>> GetSettingsAsync(Guid memberId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateSettingAsync(Guid memberId, Guid notificationTypeId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> ListTypesAsync(CancellationToken ct = default);
}
