namespace WorkService.Domain.Interfaces.Services.RiskRegisters;

public interface IRiskRegisterService
{
    Task<object> CreateAsync(Guid orgId, Guid userId, object request, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid riskId, object request, CancellationToken ct = default);
    Task DeleteAsync(Guid riskId, CancellationToken ct = default);
    Task<object> ListAsync(Guid orgId, Guid projectId, Guid? sprintId,
        string? severity, string? mitigationStatus,
        int page, int pageSize, CancellationToken ct = default);
}
