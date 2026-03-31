using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories.TeamMembers;

public interface ITeamMemberRepository
{
    Task<TeamMember?> GetByIdAsync(Guid memberId, CancellationToken ct = default);
    Task<TeamMember?> GetByEmailAsync(Guid organizationId, string email, CancellationToken ct = default);
    Task<TeamMember?> GetByEmailGlobalAsync(string email, CancellationToken ct = default);
    Task<TeamMember> AddAsync(TeamMember member, CancellationToken ct = default);
    Task UpdateAsync(TeamMember member, CancellationToken ct = default);
    Task<(IEnumerable<TeamMember> Items, int TotalCount)> ListAsync(Guid organizationId, int page, int pageSize, Guid? departmentId, string? role, string? status, string? availability, CancellationToken ct = default);
    Task<int> CountOrgAdminsAsync(Guid organizationId, CancellationToken ct = default);
    Task<int> GetNextSequentialNumberAsync(Guid organizationId, string departmentCode, CancellationToken ct = default);
}
