using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories;

public interface IActivityLogRepository
{
    Task<ActivityLog> AddAsync(ActivityLog log, CancellationToken ct = default);
    Task<IEnumerable<ActivityLog>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
