using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories;

public interface IArchivedAuditLogRepository
{
    Task AddRangeAsync(IEnumerable<ArchivedAuditLog> logs, CancellationToken ct = default);
    Task<(IEnumerable<ArchivedAuditLog> Items, int TotalCount)> QueryAsync(Guid organizationId, string? serviceName, string? action, string? entityType, string? userId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
}
