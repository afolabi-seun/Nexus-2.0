namespace UtilityService.Domain.Interfaces.Services;

public interface IAuditLogService
{
    Task<object> CreateAsync(object request, CancellationToken ct = default);
    Task<object> QueryAsync(Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default);
    Task<object> QueryArchiveAsync(Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default);
}
