using UtilityService.Domain.Results;

namespace UtilityService.Domain.Interfaces.Services.AuditLogs;

public interface IAuditLogService
{
    Task<ServiceResult<object>> CreateAsync(object request, CancellationToken ct = default);
    Task<ServiceResult<object>> QueryAsync(Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<object>> QueryArchiveAsync(Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default);
}
