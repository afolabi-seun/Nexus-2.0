using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.TimePolicies;

public interface ITimePolicyRepository : IGenericRepository<TimePolicy>
{
    Task<TimePolicy?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}
