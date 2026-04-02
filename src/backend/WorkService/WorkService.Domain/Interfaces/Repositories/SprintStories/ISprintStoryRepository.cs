using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.SprintStories;

public interface ISprintStoryRepository : IGenericRepository<SprintStory>
{
    Task<SprintStory?> GetAsync(Guid sprintId, Guid storyId, CancellationToken ct = default);
    Task<IEnumerable<SprintStory>> ListBySprintAsync(Guid sprintId, CancellationToken ct = default);
}
