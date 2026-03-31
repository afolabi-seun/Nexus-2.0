namespace ProfileService.Domain.Interfaces.Services.NotificationSettings;

public interface INotificationSettingService
{
    Task<IEnumerable<object>> GetSettingsAsync(Guid memberId, CancellationToken ct = default);
    Task UpdateSettingAsync(Guid memberId, Guid notificationTypeId, object request, CancellationToken ct = default);
    Task<IEnumerable<object>> ListTypesAsync(CancellationToken ct = default);
}
