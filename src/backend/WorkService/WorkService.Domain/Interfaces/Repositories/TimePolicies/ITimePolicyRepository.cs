using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.TimePolicies;

public interface ITimePolicyRepository
{
    Task<TimePolicy?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<TimePolicy> AddAsync(TimePolicy policy, CancellationToken ct = default);
    Task UpdateAsync(TimePolicy policy, CancellationToken ct = default);
}
