using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.Labels;

public interface ILabelRepository : IGenericRepository<Label>
{
    Task<Label?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default);
    Task<IEnumerable<Label>> ListAsync(Guid organizationId, CancellationToken ct = default);
}
