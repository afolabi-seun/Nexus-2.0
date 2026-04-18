namespace ProfileService.Domain.Interfaces.Services.TeamMembers;

public interface ITeamMemberService
{
    Task<object> ListAsync(Guid organizationId, int page, int pageSize, string? departmentId, string? role, string? status, string? availability, CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid memberId, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid memberId, object request, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid memberId, string newStatus, CancellationToken ct = default);
    Task UpdateAvailabilityAsync(Guid memberId, string availability, CancellationToken ct = default);
    Task AddToDepartmentAsync(Guid memberId, object request, CancellationToken ct = default);
    Task RemoveFromDepartmentAsync(Guid memberId, Guid departmentId, CancellationToken ct = default);
    Task ChangeDepartmentRoleAsync(Guid memberId, Guid departmentId, object request, CancellationToken ct = default);
    Task<object> GetByEmailAsync(string email, CancellationToken ct = default);
    Task UpdatePasswordAsync(Guid memberId, string passwordHash, CancellationToken ct = default);
    Task<object> SearchAsync(Guid organizationId, string query, int page, int pageSize, CancellationToken ct = default);
}
