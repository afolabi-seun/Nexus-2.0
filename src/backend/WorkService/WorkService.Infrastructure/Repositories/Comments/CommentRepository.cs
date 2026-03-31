using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Comments;
using WorkService.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.Comments;

public class CommentRepository : ICommentRepository
{
    private readonly WorkDbContext _db;

    public CommentRepository(WorkDbContext db) => _db = db;

    public async Task<Comment?> GetByIdAsync(Guid commentId, CancellationToken ct = default)
        => await _db.Comments.FirstOrDefaultAsync(c => c.CommentId == commentId, ct);

    public async Task<Comment> AddAsync(Comment comment, CancellationToken ct = default)
    {
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);
        return comment;
    }

    public async Task UpdateAsync(Comment comment, CancellationToken ct = default)
    {
        _db.Comments.Update(comment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Comment>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => await _db.Comments
            .Where(c => c.EntityType == entityType && c.EntityId == entityId)
            .OrderBy(c => c.DateCreated)
            .ToListAsync(ct);
}
