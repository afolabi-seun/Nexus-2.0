using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.TaskTypeRefs;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Repositories.Generics;

namespace UtilityService.Infrastructure.Repositories.TaskTypeRefs;

public class TaskTypeRefRepository : GenericRepository<TaskTypeRef>, ITaskTypeRefRepository
{
    private readonly UtilityDbContext _db;

    public TaskTypeRefRepository(UtilityDbContext db) : base(db) => _db = db;

    public async Task<IEnumerable<TaskTypeRef>> ListAsync(CancellationToken ct = default)
        => await _db.TaskTypeRefs.AsNoTracking().OrderBy(e => e.TypeName).ToListAsync(ct);

    public async Task<bool> ExistsAsync(string typeName, CancellationToken ct = default)
        => await _db.TaskTypeRefs.AnyAsync(e => e.TypeName == typeName, ct);
}
