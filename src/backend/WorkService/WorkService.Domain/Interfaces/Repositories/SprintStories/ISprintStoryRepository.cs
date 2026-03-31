using WorkService.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.SprintStories;

public interface ISprintStoryRepository
{
    Task<SprintStory?> GetAsync(Guid sprintId, Guid storyId, CancellationToken ct = default);
    Task<SprintStory> AddAsync(SprintStory sprintStory, CancellationToken ct = default);
    Task UpdateAsync(SprintStory sprintStory, CancellationToken ct = default);
    Task<IEnumerable<SprintStory>> ListBySprintAsync(Guid sprintId, CancellationToken ct = default);
}
