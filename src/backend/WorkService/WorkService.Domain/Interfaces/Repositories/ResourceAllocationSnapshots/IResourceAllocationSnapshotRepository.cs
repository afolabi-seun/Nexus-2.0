using WorkService.Domain.Entities;

namespace WorkService.Domain.Interfaces.Repositories.ResourceAllocationSnapshots;

public interface IResourceAllocationSnapshotRepository
{
    Task<ResourceAllocationSnapshot> AddOrUpdateAsync(
        ResourceAllocationSnapshot snapshot, CancellationToken ct = default);
    Task<IEnumerable<ResourceAllocationSnapshot>> GetByProjectAsync(
        Guid projectId, DateTime periodStart, DateTime periodEnd,
        CancellationToken ct = default);
}
