using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.TeamMembers;

public interface ITeamMemberService
{
    Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, string? departmentId, string? role, string? status, string? availability, CancellationToken ct = default);
    Task<ServiceResult<object>> GetByIdAsync(Guid memberId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid memberId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateStatusAsync(Guid memberId, string newStatus, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAvailabilityAsync(Guid memberId, string availability, CancellationToken ct = default);
    Task<ServiceResult<object>> AddToDepartmentAsync(Guid memberId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> RemoveFromDepartmentAsync(Guid memberId, Guid departmentId, CancellationToken ct = default);
    Task<ServiceResult<object>> ChangeDepartmentRoleAsync(Guid memberId, Guid departmentId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdatePasswordAsync(Guid memberId, string passwordHash, CancellationToken ct = default);
    Task<ServiceResult<object>> SearchAsync(Guid organizationId, string query, int page, int pageSize, CancellationToken ct = default);
}
