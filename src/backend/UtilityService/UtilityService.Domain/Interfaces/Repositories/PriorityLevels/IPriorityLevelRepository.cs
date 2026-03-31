using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories.PriorityLevels;

public interface IPriorityLevelRepository
{
    Task<PriorityLevel?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<PriorityLevel> AddAsync(PriorityLevel priorityLevel, CancellationToken ct = default);
    Task<IEnumerable<PriorityLevel>> ListAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<PriorityLevel> levels, CancellationToken ct = default);
    Task<bool> ExistsAsync(string name, CancellationToken ct = default);
}
