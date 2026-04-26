using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.ActivityLog;

public interface IActivityLogService
{
    Task LogAsync(Guid organizationId, string entityType, Guid entityId, string storyKey, string action, Guid actorId, string actorName, string? oldValue, string? newValue, string description, CancellationToken ct = default);
    Task<ServiceResult<object>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task<ServiceResult<object>> GetOrganizationFeedAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default);
}
