namespace ProfileService.Domain.Interfaces.Services;

public interface IDepartmentService
{
    Task<object> CreateAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<object> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid departmentId, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid departmentId, object request, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid departmentId, string newStatus, CancellationToken ct = default);
    Task<object> ListMembersAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default);
    Task<object> GetPreferencesAsync(Guid departmentId, CancellationToken ct = default);
    Task<object> UpdatePreferencesAsync(Guid departmentId, object request, CancellationToken ct = default);
}
