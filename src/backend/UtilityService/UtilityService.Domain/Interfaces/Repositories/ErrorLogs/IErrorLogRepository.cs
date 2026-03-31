using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories.ErrorLogs;

public interface IErrorLogRepository
{
    Task<ErrorLog> AddAsync(ErrorLog errorLog, CancellationToken ct = default);
    Task<(IEnumerable<ErrorLog> Items, int TotalCount)> QueryAsync(Guid organizationId, string? serviceName, string? errorCode, string? severity, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
}
