using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.DepartmentMembers;

public class DepartmentMemberRepository : GenericRepository<DepartmentMember>, IDepartmentMemberRepository
{
    private readonly ProfileDbContext _db;

    public DepartmentMemberRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<DepartmentMember?> GetAsync(Guid memberId, Guid departmentId, CancellationToken ct = default)
    {
        return await _db.DepartmentMembers
            .Include(dm => dm.Role)
            .Include(dm => dm.Department)
            .FirstOrDefaultAsync(dm => dm.TeamMemberId == memberId && dm.DepartmentId == departmentId, ct);
    }

    public async Task<IEnumerable<DepartmentMember>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _db.DepartmentMembers
            .Include(dm => dm.Department)
            .Include(dm => dm.Role)
            .Where(dm => dm.TeamMemberId == memberId)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<DepartmentMember> Items, int TotalCount)> ListByDepartmentAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.DepartmentMembers
            .Include(dm => dm.TeamMember)
            .Include(dm => dm.Role)
            .Where(dm => dm.DepartmentId == departmentId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(dm => dm.DateJoined)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
