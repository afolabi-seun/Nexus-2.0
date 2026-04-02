using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.Generics;

namespace UtilityService.Domain.Interfaces.Repositories.PriorityLevels;

public interface IPriorityLevelRepository : IGenericRepository<PriorityLevel>
{
    Task<PriorityLevel?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IEnumerable<PriorityLevel>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string name, CancellationToken ct = default);
}
