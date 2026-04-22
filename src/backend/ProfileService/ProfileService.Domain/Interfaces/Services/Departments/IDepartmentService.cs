using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.Departments;

public interface IDepartmentService
{
    Task<ServiceResult<object>> CreateAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<object>> GetByIdAsync(Guid departmentId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid departmentId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateStatusAsync(Guid departmentId, string newStatus, CancellationToken ct = default);
    Task<ServiceResult<object>> ListMembersAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<object>> GetPreferencesAsync(Guid departmentId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdatePreferencesAsync(Guid departmentId, object request, CancellationToken ct = default);
}
