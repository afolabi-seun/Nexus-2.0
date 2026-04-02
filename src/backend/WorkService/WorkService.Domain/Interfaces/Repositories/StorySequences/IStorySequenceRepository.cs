using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.StorySequences;

public interface IStorySequenceRepository : IGenericRepository<StorySequence>
{
    Task InitializeAsync(Guid projectId, CancellationToken ct = default);
    Task<long> IncrementAndGetAsync(Guid projectId, CancellationToken ct = default);
}
