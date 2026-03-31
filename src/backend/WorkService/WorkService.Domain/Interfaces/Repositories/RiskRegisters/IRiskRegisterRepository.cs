using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.RiskRegisters;

public interface IRiskRegisterRepository
{
    Task<RiskRegister?> GetByIdAsync(Guid riskRegisterId, CancellationToken ct = default);
    Task<RiskRegister> AddAsync(RiskRegister risk, CancellationToken ct = default);
    Task UpdateAsync(RiskRegister risk, CancellationToken ct = default);
    Task<(IEnumerable<RiskRegister> Items, int TotalCount)> ListAsync(
        Guid organizationId, Guid projectId, Guid? sprintId,
        string? severity, string? mitigationStatus,
        int page, int pageSize, CancellationToken ct = default);
    Task<int> CountActiveByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<RiskRegister>> GetActiveByProjectAsync(Guid projectId, CancellationToken ct = default);
}
