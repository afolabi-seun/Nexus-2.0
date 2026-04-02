using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.PriorityLevels;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Repositories.Generics;

namespace UtilityService.Infrastructure.Repositories.PriorityLevels;

public class PriorityLevelRepository : GenericRepository<PriorityLevel>, IPriorityLevelRepository
{
    private readonly UtilityDbContext _db;

    public PriorityLevelRepository(UtilityDbContext db) : base(db) => _db = db;

    public async Task<PriorityLevel?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _db.PriorityLevels.FirstOrDefaultAsync(e => e.Name == name, ct);

    public async Task<IEnumerable<PriorityLevel>> ListAsync(CancellationToken ct = default)
        => await _db.PriorityLevels.AsNoTracking().OrderBy(e => e.SortOrder).ToListAsync(ct);

    public async Task<bool> ExistsAsync(string name, CancellationToken ct = default)
        => await _db.PriorityLevels.AnyAsync(e => e.Name == name, ct);
}
