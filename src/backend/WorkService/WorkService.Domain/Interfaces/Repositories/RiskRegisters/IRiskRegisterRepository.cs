using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.RiskRegisters;

public interface IRiskRegisterRepository : IGenericRepository<RiskRegister>
{
    Task<(IEnumerable<RiskRegister> Items, int TotalCount)> ListAsync(
        Guid organizationId, Guid projectId, Guid? sprintId,
        string? severity, string? mitigationStatus,
        int page, int pageSize, CancellationToken ct = default);
    Task<int> CountActiveByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<RiskRegister>> GetActiveByProjectAsync(Guid projectId, CancellationToken ct = default);
}
