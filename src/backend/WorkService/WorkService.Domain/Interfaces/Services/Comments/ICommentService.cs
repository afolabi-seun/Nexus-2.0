namespace WorkService.Domain.Interfaces.Services.Comments;

public interface ICommentService
{
    Task<object> CreateAsync(Guid organizationId, Guid authorId, object request, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid commentId, Guid userId, string content, CancellationToken ct = default);
    Task DeleteAsync(Guid commentId, Guid userId, string userRole, CancellationToken ct = default);
    Task<object> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
