namespace WorkService.Domain.Interfaces.Services.Projects;

public interface IProjectService
{
    Task<object> CreateAsync(Guid organizationId, Guid creatorId, object request, CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid projectId, CancellationToken ct = default);
    Task<object> ListAsync(Guid organizationId, int page, int pageSize, string? status, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid projectId, object request, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid projectId, string newStatus, CancellationToken ct = default);
}
