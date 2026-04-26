using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.CostSnapshots;

public interface ICostSnapshotService
{
    Task<ServiceResult<object>> ListByProjectAsync(Guid projectId, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct = default);
}
