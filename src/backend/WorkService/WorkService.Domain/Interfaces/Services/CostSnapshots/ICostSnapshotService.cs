namespace WorkService.Domain.Interfaces.Services.CostSnapshots;

public interface ICostSnapshotService
{
    Task<object> ListByProjectAsync(Guid projectId, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct = default);
}
