using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories.AuditLogs;

public interface IAuditLogRepository
{
    Task<AuditLog> AddAsync(AuditLog auditLog, CancellationToken ct = default);
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> QueryAsync(Guid organizationId, string? serviceName, string? action, string? entityType, string? userId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
}
