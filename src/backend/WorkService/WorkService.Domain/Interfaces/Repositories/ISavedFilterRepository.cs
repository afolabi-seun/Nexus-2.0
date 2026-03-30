using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories;

public interface ISavedFilterRepository
{
    Task<SavedFilter?> GetByIdAsync(Guid filterId, CancellationToken ct = default);
    Task<SavedFilter> AddAsync(SavedFilter filter, CancellationToken ct = default);
    Task RemoveAsync(SavedFilter filter, CancellationToken ct = default);
    Task<IEnumerable<SavedFilter>> ListByMemberAsync(Guid organizationId, Guid memberId, CancellationToken ct = default);
}
