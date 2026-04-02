using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.NotificationTypes;

public interface INotificationTypeRepository : IGenericRepository<NotificationType>
{
    Task<IEnumerable<NotificationType>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string typeName, CancellationToken ct = default);
}
