namespace WorkService.Domain.Interfaces.Services;

public interface IActivityLogService
{
    Task LogAsync(Guid organizationId, string entityType, Guid entityId, string storyKey, string action, Guid actorId, string actorName, string? oldValue, string? newValue, string description, CancellationToken ct = default);
    Task<object> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
