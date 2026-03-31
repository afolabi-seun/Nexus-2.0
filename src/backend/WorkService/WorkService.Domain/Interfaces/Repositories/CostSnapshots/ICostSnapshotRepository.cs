using WorkService.Domain.Entities;

namespace WorkService.Domain.Interfaces.Repositories.CostSnapshots;

public interface ICostSnapshotRepository
{
    Task<CostSnapshot> AddOrUpdateAsync(CostSnapshot snapshot, CancellationToken ct = default);
    Task<(IEnumerable<CostSnapshot> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct = default);
}
