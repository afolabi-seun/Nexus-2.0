using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.TeamMembers;

public interface ITeamMemberRepository : IGenericRepository<TeamMember>
{
    Task<TeamMember?> GetByEmailAsync(Guid organizationId, string email, CancellationToken ct = default);
    Task<TeamMember?> GetByEmailGlobalAsync(string email, CancellationToken ct = default);
    Task<(IEnumerable<TeamMember> Items, int TotalCount)> ListAsync(Guid organizationId, int page, int pageSize, Guid? departmentId, string? role, string? status, string? availability, CancellationToken ct = default);
    Task<int> CountOrgAdminsAsync(Guid organizationId, CancellationToken ct = default);
    Task<int> GetNextSequentialNumberAsync(Guid organizationId, string departmentCode, CancellationToken ct = default);
    Task<(IEnumerable<TeamMember> Items, int TotalCount)> SearchAsync(Guid organizationId, string query, int page, int pageSize, CancellationToken ct = default);
}
