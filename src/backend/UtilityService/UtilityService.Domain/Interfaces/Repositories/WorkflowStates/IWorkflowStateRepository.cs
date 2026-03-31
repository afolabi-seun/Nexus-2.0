using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories;

public interface IWorkflowStateRepository
{
    Task<IEnumerable<WorkflowState>> ListAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<WorkflowState> states, CancellationToken ct = default);
    Task<bool> ExistsAsync(string entityType, string stateName, CancellationToken ct = default);
}
