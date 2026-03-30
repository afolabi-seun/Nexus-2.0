using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.DepartmentMembers;

public class DepartmentMemberRepository : IDepartmentMemberRepository
{
    private readonly ProfileDbContext _context;

    public DepartmentMemberRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<DepartmentMember?> GetAsync(Guid memberId, Guid departmentId, CancellationToken ct = default)
    {
        return await _context.DepartmentMembers
            .Include(dm => dm.Role)
            .Include(dm => dm.Department)
            .FirstOrDefaultAsync(dm => dm.TeamMemberId == memberId && dm.DepartmentId == departmentId, ct);
    }

    public async Task<DepartmentMember> AddAsync(DepartmentMember departmentMember, CancellationToken ct = default)
    {
        await _context.DepartmentMembers.AddAsync(departmentMember, ct);
        await _context.SaveChangesAsync(ct);
        return departmentMember;
    }

    public async Task RemoveAsync(DepartmentMember departmentMember, CancellationToken ct = default)
    {
        _context.DepartmentMembers.Remove(departmentMember);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DepartmentMember departmentMember, CancellationToken ct = default)
    {
        _context.DepartmentMembers.Update(departmentMember);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<DepartmentMember>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.DepartmentMembers
            .Include(dm => dm.Department)
            .Include(dm => dm.Role)
            .Where(dm => dm.TeamMemberId == memberId)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<DepartmentMember> Items, int TotalCount)> ListByDepartmentAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.DepartmentMembers
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
