using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;

namespace ProfileService.Infrastructure.Repositories.Departments;

public class DepartmentRepository : GenericRepository<Department>, IDepartmentRepository
{
    private readonly ProfileDbContext _db;

    public DepartmentRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Department?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default)
    {
        return await _db.Departments
            .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.DepartmentName == name, ct);
    }

    public async Task<Department?> GetByCodeAsync(Guid organizationId, string code, CancellationToken ct = default)
    {
        return await _db.Departments
            .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.DepartmentCode == code, ct);
    }

    public async Task<(IEnumerable<Department> Items, int TotalCount)> ListByOrganizationAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Departments.Where(d => d.OrganizationId == organizationId);
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
        return await _db.DepartmentMembers
            .Where(dm => dm.DepartmentId == departmentId)
            .Join(_db.TeamMembers,
                dm => dm.TeamMemberId,
                tm => tm.TeamMemberId,
                (dm, tm) => tm)
            .CountAsync(tm => tm.FlgStatus == EntityStatuses.Active, ct);
    }
}
