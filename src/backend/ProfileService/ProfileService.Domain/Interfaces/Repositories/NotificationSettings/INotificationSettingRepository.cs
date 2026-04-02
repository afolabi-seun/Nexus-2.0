using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.NotificationSettings;

public interface INotificationSettingRepository : IGenericRepository<NotificationSetting>
{
    Task<IEnumerable<NotificationSetting>> GetByMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<NotificationSetting?> GetAsync(Guid memberId, Guid notificationTypeId, CancellationToken ct = default);
}
