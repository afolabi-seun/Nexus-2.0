using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;

namespace WorkService.Domain.Interfaces.Repositories.ActivityLogs;

public interface IActivityLogRepository : IGenericRepository<ActivityLog>
{
    Task<IEnumerable<ActivityLog>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
