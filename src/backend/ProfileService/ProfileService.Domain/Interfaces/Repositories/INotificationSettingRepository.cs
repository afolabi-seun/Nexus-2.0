using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories;

public interface INotificationSettingRepository
{
    Task<IEnumerable<NotificationSetting>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<NotificationSetting?> GetAsync(Guid memberId, Guid notificationTypeId, CancellationToken ct = default);
    Task AddAsync(NotificationSetting setting, CancellationToken ct = default);
    Task UpdateAsync(NotificationSetting setting, CancellationToken ct = default);
}
