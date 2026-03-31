using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.TaskTypeRefs;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Repositories.TaskTypeRefs;

public class TaskTypeRefRepository : ITaskTypeRefRepository
{
    private readonly UtilityDbContext _context;

    public TaskTypeRefRepository(UtilityDbContext context) => _context = context;

    public async Task<IEnumerable<TaskTypeRef>> ListAsync(CancellationToken ct = default)
        => await _context.TaskTypeRefs.AsNoTracking().OrderBy(e => e.TypeName).ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<TaskTypeRef> types, CancellationToken ct = default)
    {
        _context.TaskTypeRefs.AddRange(types);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string typeName, CancellationToken ct = default)
        => await _context.TaskTypeRefs.AnyAsync(e => e.TypeName == typeName, ct);
}
