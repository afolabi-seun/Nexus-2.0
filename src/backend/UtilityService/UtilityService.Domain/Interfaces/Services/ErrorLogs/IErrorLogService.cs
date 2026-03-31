namespace UtilityService.Domain.Interfaces.Services.ErrorLogs;

public interface IErrorLogService
{
    Task<object> CreateAsync(object request, CancellationToken ct = default);
    Task<object> QueryAsync(Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default);
}
