using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Export;

public interface IExportService
{
    Task<ServiceResult<byte[]>> ExportStoriesCsvAsync(Guid organizationId, Guid? projectId, Guid? sprintId, CancellationToken ct = default);
    Task<ServiceResult<byte[]>> ExportTimeEntriesCsvAsync(Guid organizationId, Guid? projectId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
}
