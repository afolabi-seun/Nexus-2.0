using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;

namespace WorkService.Domain.Interfaces.Repositories.ResourceAllocationSnapshots;

public interface IResourceAllocationSnapshotRepository : IGenericRepository<ResourceAllocationSnapshot>
{
    Task<ResourceAllocationSnapshot> AddOrUpdateAsync(
        ResourceAllocationSnapshot snapshot, CancellationToken ct = default);
    Task<IEnumerable<ResourceAllocationSnapshot>> GetByProjectAsync(
        Guid projectId, DateTime periodStart, DateTime periodEnd,
        CancellationToken ct = default);
}
