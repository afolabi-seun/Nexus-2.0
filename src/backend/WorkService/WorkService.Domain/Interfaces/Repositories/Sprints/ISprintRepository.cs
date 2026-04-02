using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.Sprints;

public interface ISprintRepository : IGenericRepository<Sprint>
{
    Task<(IEnumerable<Sprint> Items, int TotalCount)> ListAsync(Guid organizationId, int page, int pageSize, string? status, Guid? projectId, CancellationToken ct = default);
    Task<Sprint?> GetActiveByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<Sprint>> GetCompletedAsync(Guid organizationId, int count, CancellationToken ct = default);
    Task<bool> HasOverlappingAsync(Guid projectId, DateTime startDate, DateTime endDate, Guid? excludeSprintId, CancellationToken ct = default);
}
