using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.Generics;

namespace UtilityService.Domain.Interfaces.Repositories.ArchivedAuditLogs;

public interface IArchivedAuditLogRepository : IGenericRepository<ArchivedAuditLog>
{
    Task<(IEnumerable<ArchivedAuditLog> Items, int TotalCount)> QueryAsync(Guid organizationId, string? serviceName, string? action, string? entityType, string? userId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
}
