using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Infrastructure.Data;

namespace ProfileService.Infrastructure.Repositories.Departments;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ProfileDbContext _context;

    public DepartmentRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Department?> GetByIdAsync(Guid departmentId, CancellationToken ct = default)
    {
        return await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == departmentId, ct);
    }

    public async Task<Department?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.DepartmentName == name, ct);
    }

    public async Task<Department?> GetByCodeAsync(Guid organizationId, string code, CancellationToken ct = default)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.DepartmentCode == code, ct);
    }

    public async Task<Department> AddAsync(Department department, CancellationToken ct = default)
    {
        await _context.Departments.AddAsync(department, ct);
        await _context.SaveChangesAsync(ct);
        return department;
    }

    public async Task AddRangeAsync(IEnumerable<Department> departments, CancellationToken ct = default)
    {
        await _context.Departments.AddRangeAsync(departments, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Department department, CancellationToken ct = default)
    {
        _context.Departments.Update(department);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<Department> Items, int TotalCount)> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Departments.Where(d => d.OrganizationId == organizationId);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.DepartmentName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<int> GetActiveMemberCountAsync(Guid departmentId, CancellationToken ct = default)
    {
        return await _context.DepartmentMembers
            .Where(dm => dm.DepartmentId == departmentId)
            .Join(_context.TeamMembers,
                dm => dm.TeamMemberId,
                tm => tm.TeamMemberId,
                (dm, tm) => tm)
            .CountAsync(tm => tm.FlgStatus == EntityStatuses.Active, ct);
    }
}
