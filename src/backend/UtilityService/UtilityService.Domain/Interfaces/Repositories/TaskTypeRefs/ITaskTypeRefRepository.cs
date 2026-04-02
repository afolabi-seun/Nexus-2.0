using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.Generics;

namespace UtilityService.Domain.Interfaces.Repositories.TaskTypeRefs;

public interface ITaskTypeRefRepository : IGenericRepository<TaskTypeRef>
{
    Task<IEnumerable<TaskTypeRef>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string typeName, CancellationToken ct = default);
}
