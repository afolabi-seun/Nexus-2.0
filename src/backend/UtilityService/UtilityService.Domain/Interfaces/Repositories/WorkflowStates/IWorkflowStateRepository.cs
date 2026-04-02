using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.Generics;

namespace UtilityService.Domain.Interfaces.Repositories.WorkflowStates;

public interface IWorkflowStateRepository : IGenericRepository<WorkflowState>
{
    Task<IEnumerable<WorkflowState>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string entityType, string stateName, CancellationToken ct = default);
}
