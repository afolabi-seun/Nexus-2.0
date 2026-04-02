using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.Generics;

namespace UtilityService.Domain.Interfaces.Repositories.ErrorLogs;

public interface IErrorLogRepository : IGenericRepository<ErrorLog>
{
    Task<(IEnumerable<ErrorLog> Items, int TotalCount)> QueryAsync(Guid organizationId, string? serviceName, string? errorCode, string? severity, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct = default);
}
