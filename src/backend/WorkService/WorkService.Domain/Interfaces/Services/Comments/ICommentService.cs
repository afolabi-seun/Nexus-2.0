using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Comments;

public interface ICommentService
{
    Task<ServiceResult<object>> CreateAsync(Guid organizationId, Guid authorId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid commentId, Guid userId, string content, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(Guid commentId, Guid userId, string userRole, CancellationToken ct = default);
    Task<ServiceResult<object>> ListByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
