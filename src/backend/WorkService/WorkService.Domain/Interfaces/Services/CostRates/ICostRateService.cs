using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.CostRates;

public interface ICostRateService
{
    Task<ServiceResult<object>> CreateAsync(Guid orgId, Guid userId, string userRole, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid costRateId, Guid userId, string userRole, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(Guid costRateId, Guid userId, string userRole, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid orgId, string? rateType, Guid? memberId,
        Guid? departmentId, string? roleName, int page, int pageSize, CancellationToken ct = default);
}
