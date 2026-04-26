using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Projects;

public interface IProjectService
{
    Task<ServiceResult<object>> CreateAsync(Guid organizationId, Guid creatorId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> GetByIdAsync(Guid projectId, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, string? status, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid projectId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateStatusAsync(Guid projectId, string newStatus, CancellationToken ct = default);
}
