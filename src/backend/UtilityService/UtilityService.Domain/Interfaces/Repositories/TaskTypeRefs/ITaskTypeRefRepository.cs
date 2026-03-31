using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories.TaskTypeRefs;

public interface ITaskTypeRefRepository
{
    Task<IEnumerable<TaskTypeRef>> ListAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TaskTypeRef> types, CancellationToken ct = default);
    Task<bool> ExistsAsync(string typeName, CancellationToken ct = default);
}
