using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories;

public interface INotificationTypeRepository
{
    Task<IEnumerable<NotificationType>> ListAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<NotificationType> types, CancellationToken ct = default);
    Task<bool> ExistsAsync(string typeName, CancellationToken ct = default);
}
