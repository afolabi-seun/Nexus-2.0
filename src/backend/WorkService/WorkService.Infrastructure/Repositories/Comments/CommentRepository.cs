using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Comments;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.Comments;

public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    private readonly WorkDbContext _db;

    public CommentRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Comment>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await _db.Comments
            .Where(c => c.EntityType == entityType && c.EntityId == entityId)
            .OrderBy(c => c.DateCreated)
            .ToListAsync(ct);
}
