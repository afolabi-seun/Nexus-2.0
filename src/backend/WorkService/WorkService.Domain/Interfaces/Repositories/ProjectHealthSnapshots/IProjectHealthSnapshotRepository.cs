using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;

namespace WorkService.Domain.Interfaces.Repositories.ProjectHealthSnapshots;

public interface IProjectHealthSnapshotRepository : IGenericRepository<ProjectHealthSnapshot>
{
    Task<ProjectHealthSnapshot?> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<ProjectHealthSnapshot>> GetHistoryByProjectAsync(
        Guid projectId, int count, CancellationToken ct = default);
}
