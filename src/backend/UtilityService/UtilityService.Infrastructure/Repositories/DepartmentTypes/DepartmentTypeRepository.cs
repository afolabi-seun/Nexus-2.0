using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.DepartmentTypes;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Repositories.Generics;

namespace UtilityService.Infrastructure.Repositories.DepartmentTypes;

public class DepartmentTypeRepository : GenericRepository<DepartmentType>, IDepartmentTypeRepository
{
    private readonly UtilityDbContext _db;

    public DepartmentTypeRepository(UtilityDbContext db) : base(db) => _db = db;

    public async Task<DepartmentType?> GetByNameAsync(string typeName, CancellationToken ct = default)
        => await _db.DepartmentTypes.FirstOrDefaultAsync(e => e.TypeName == typeName, ct);

    public async Task<DepartmentType?> GetByCodeAsync(string typeCode, CancellationToken ct = default)
        => await _db.DepartmentTypes.FirstOrDefaultAsync(e => e.TypeCode == typeCode, ct);

    public async Task<IEnumerable<DepartmentType>> ListAsync(CancellationToken ct = default)
        => await _db.DepartmentTypes.AsNoTracking().OrderBy(e => e.TypeName).ToListAsync(ct);

    public async Task<bool> ExistsAsync(string typeName, CancellationToken ct = default)
        => await _db.DepartmentTypes.AnyAsync(e => e.TypeName == typeName, ct);
}
