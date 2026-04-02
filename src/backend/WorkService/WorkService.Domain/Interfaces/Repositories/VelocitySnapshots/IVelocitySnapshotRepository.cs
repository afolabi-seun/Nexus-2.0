using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;

namespace WorkService.Domain.Interfaces.Repositories.VelocitySnapshots;

public interface IVelocitySnapshotRepository : IGenericRepository<VelocitySnapshot>
{
    Task<VelocitySnapshot> AddOrUpdateAsync(VelocitySnapshot snapshot, CancellationToken ct = default);
    Task<IEnumerable<VelocitySnapshot>> GetByProjectAsync(
        Guid projectId, int count, CancellationToken ct = default);
    Task<VelocitySnapshot?> GetBySprintAsync(Guid sprintId, CancellationToken ct = default);
}
