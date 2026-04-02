using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.SavedFilters;

public interface ISavedFilterRepository : IGenericRepository<SavedFilter>
{
    Task<IEnumerable<SavedFilter>> ListByMemberAsync(Guid organizationId, Guid memberId, CancellationToken ct = default);
}
