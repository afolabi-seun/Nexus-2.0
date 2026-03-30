using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Repositories.DepartmentTypes;

public class DepartmentTypeRepository : IDepartmentTypeRepository
{
    private readonly UtilityDbContext _context;

    public DepartmentTypeRepository(UtilityDbContext context) => _context = context;

    public async Task<DepartmentType?> GetByNameAsync(string typeName, CancellationToken ct = default)
        => await _context.DepartmentTypes.FirstOrDefaultAsync(e => e.TypeName == typeName, ct);

    public async Task<DepartmentType?> GetByCodeAsync(string typeCode, CancellationToken ct = default)
        => await _context.DepartmentTypes.FirstOrDefaultAsync(e => e.TypeCode == typeCode, ct);

    public async Task<DepartmentType> AddAsync(DepartmentType departmentType, CancellationToken ct = default)
    {
        _context.DepartmentTypes.Add(departmentType);
        await _context.SaveChangesAsync(ct);
        return departmentType;
    }

    public async Task<IEnumerable<DepartmentType>> ListAsync(CancellationToken ct = default)
        => await _context.DepartmentTypes.AsNoTracking().OrderBy(e => e.TypeName).ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<DepartmentType> types, CancellationToken ct = default)
    {
        _context.DepartmentTypes.AddRange(types);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string typeName, CancellationToken ct = default)
        => await _context.DepartmentTypes.AnyAsync(e => e.TypeName == typeName, ct);
}
