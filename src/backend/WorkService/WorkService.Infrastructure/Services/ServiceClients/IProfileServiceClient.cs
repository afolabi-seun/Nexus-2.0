using WorkService.Application.Contracts;

namespace WorkService.Infrastructure.Services.ServiceClients;

public interface IProfileServiceClient
{
    Task<OrganizationSettingsResponse> GetOrganizationSettingsAsync(Guid organizationId, CancellationToken ct = default);
    Task<TeamMemberResponse?> GetTeamMemberAsync(Guid memberId, CancellationToken ct = default);
    Task<IEnumerable<TeamMemberResponse>> GetDepartmentMembersAsync(Guid departmentId, CancellationToken ct = default);
    Task<DepartmentResponse?> GetDepartmentByCodeAsync(Guid organizationId, string departmentCode, CancellationToken ct = default);
    Task<TeamMemberResponse?> ResolveUserByDisplayNameAsync(Guid organizationId, string displayName, CancellationToken ct = default);
    Task<TeamMemberResponse?> ResolveUserByEmailAsync(Guid organizationId, string email, CancellationToken ct = default);
}
