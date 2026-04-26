using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.RiskRegisters;

public interface IRiskRegisterService
{
    Task<ServiceResult<object>> CreateAsync(Guid orgId, Guid userId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid riskId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(Guid riskId, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid orgId, Guid projectId, Guid? sprintId,
        string? severity, string? mitigationStatus,
        int page, int pageSize, CancellationToken ct = default);
}
