using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Domain.Interfaces.Repositories.Comments;

public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<IEnumerable<Comment>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
