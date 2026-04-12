namespace WorkService.Domain.Interfaces.Services.Export;

public interface IExportService
{
    Task<byte[]> ExportStoriesCsvAsync(Guid organizationId, Guid? projectId, Guid? sprintId, CancellationToken ct = default);
    Task<byte[]> ExportTimeEntriesCsvAsync(Guid organizationId, Guid? projectId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
}
