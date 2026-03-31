namespace WorkService.Domain.Interfaces.Services.Stories;

public interface IStoryService
{
    Task<object> CreateAsync(Guid organizationId, Guid reporterId, object request, CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid storyId, CancellationToken ct = default);
    Task<object> GetByKeyAsync(string storyKey, CancellationToken ct = default);
    Task<object> ListAsync(Guid organizationId, int page, int pageSize, Guid? projectId, string? status, string? priority, Guid? departmentId, Guid? assigneeId, Guid? sprintId, List<string>? labels, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid storyId, Guid actorId, object request, CancellationToken ct = default);
    Task DeleteAsync(Guid storyId, CancellationToken ct = default);
    Task<object> TransitionStatusAsync(Guid storyId, Guid actorId, string newStatus, CancellationToken ct = default);
    Task<object> AssignAsync(Guid storyId, Guid actorId, Guid assigneeId, string actorRole, Guid actorDepartmentId, CancellationToken ct = default);
    Task UnassignAsync(Guid storyId, Guid actorId, CancellationToken ct = default);
    Task CreateLinkAsync(Guid storyId, Guid targetStoryId, string linkType, CancellationToken ct = default);
    Task DeleteLinkAsync(Guid storyId, Guid linkId, CancellationToken ct = default);
    Task ApplyLabelAsync(Guid storyId, Guid labelId, CancellationToken ct = default);
    Task RemoveLabelAsync(Guid storyId, Guid labelId, CancellationToken ct = default);
}
