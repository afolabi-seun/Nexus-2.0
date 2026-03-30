using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories;

public interface IStorySequenceRepository
{
    Task InitializeAsync(Guid projectId, CancellationToken ct = default);
    Task<long> IncrementAndGetAsync(Guid projectId, CancellationToken ct = default);
}
