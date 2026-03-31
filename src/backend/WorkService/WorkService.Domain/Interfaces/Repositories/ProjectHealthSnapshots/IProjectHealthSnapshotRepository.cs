using WorkService.Domain.Entities;

namespace WorkService.Domain.Interfaces.Repositories.ProjectHealthSnapshots;

public interface IProjectHealthSnapshotRepository
{
    Task<ProjectHealthSnapshot> AddAsync(ProjectHealthSnapshot snapshot, CancellationToken ct = default);
    Task<ProjectHealthSnapshot?> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<ProjectHealthSnapshot>> GetHistoryByProjectAsync(
        Guid projectId, int count, CancellationToken ct = default);
}
