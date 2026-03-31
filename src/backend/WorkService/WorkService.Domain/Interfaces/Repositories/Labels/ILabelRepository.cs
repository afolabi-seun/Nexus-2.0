using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories;

public interface ILabelRepository
{
    Task<Label?> GetByIdAsync(Guid labelId, CancellationToken ct = default);
    Task<Label?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default);
    Task<Label> AddAsync(Label label, CancellationToken ct = default);
    Task UpdateAsync(Label label, CancellationToken ct = default);
    Task RemoveAsync(Label label, CancellationToken ct = default);
    Task<IEnumerable<Label>> ListAsync(Guid organizationId, CancellationToken ct = default);
}
