using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.TeamMembers;

public class TeamMemberRepository : ITeamMemberRepository
{
    private readonly ProfileDbContext _context;

    public TeamMemberRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<TeamMember?> GetByIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.TeamMembers
            .Include(t => t.DepartmentMemberships)
                .ThenInclude(dm => dm.Department)
            .Include(t => t.DepartmentMemberships)
                .ThenInclude(dm => dm.Role)
            .FirstOrDefaultAsync(t => t.TeamMemberId == memberId, ct);
    }

    public async Task<TeamMember?> GetByEmailAsync(Guid organizationId, string email, CancellationToken ct = default)
    {
        return await _context.TeamMembers
            .FirstOrDefaultAsync(t => t.OrganizationId == organizationId && t.Email == email, ct);
    }

    public async Task<TeamMember?> GetByEmailGlobalAsync(string email, CancellationToken ct = default)
    {
        return await _context.TeamMembers
            .IgnoreQueryFilters()
            .Include(t => t.DepartmentMemberships)
                .ThenInclude(dm => dm.Role)
            .FirstOrDefaultAsync(t => t.Email == email, ct);
    }

    public async Task<TeamMember> AddAsync(TeamMember member, CancellationToken ct = default)
    {
        await _context.TeamMembers.AddAsync(member, ct);
        await _context.SaveChangesAsync(ct);
        return member;
    }

    public async Task UpdateAsync(TeamMember member, CancellationToken ct = default)
    {
        _context.TeamMembers.Update(member);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<TeamMember> Items, int TotalCount)> ListAsync(
        Guid organizationId, int page, int pageSize,
        Guid? departmentId, string? role, string? status, string? availability,
        CancellationToken ct = default)
    {
        var query = _context.TeamMembers
            .Where(t => t.OrganizationId == organizationId);

        if (departmentId.HasValue)
        {
            var memberIds = _context.DepartmentMembers
                .Where(dm => dm.DepartmentId == departmentId.Value)
                .Select(dm => dm.TeamMemberId);
            query = query.Where(t => memberIds.Contains(t.TeamMemberId));
        }

        if (!string.IsNullOrEmpty(role))
        {
            var memberIdsForRole = _context.DepartmentMembers
                .Join(_context.Roles,
                    dm => dm.RoleId,
                    r => r.RoleId,
                    (dm, r) => new { dm.TeamMemberId, r.RoleName })
                .Where(x => x.RoleName == role)
                .Select(x => x.TeamMemberId);
            query = query.Where(t => memberIdsForRole.Contains(t.TeamMemberId));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.FlgStatus == status);
        }

        if (!string.IsNullOrEmpty(availability))
        {
            query = query.Where(t => t.Availability == availability);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> CountOrgAdminsAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await _context.DepartmentMembers
            .Where(dm => dm.OrganizationId == organizationId)
            .Join(_context.Roles,
                dm => dm.RoleId,
                r => r.RoleId,
                (dm, r) => new { dm.TeamMemberId, r.RoleName })
            .Where(x => x.RoleName == RoleNames.OrgAdmin)
            .Select(x => x.TeamMemberId)
            .Distinct()
            .Join(_context.TeamMembers.Where(t => t.FlgStatus == EntityStatuses.Active),
                id => id,
                t => t.TeamMemberId,
                (id, t) => id)
            .CountAsync(ct);
    }

    public async Task<int> GetNextSequentialNumberAsync(Guid organizationId, string departmentCode, CancellationToken ct = default)
    {
        var prefix = $"NXS-{departmentCode}-";
        var count = await _context.TeamMembers
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organizationId && t.ProfessionalId.StartsWith(prefix))
            .CountAsync(ct);
        return count + 1;
    }
}
