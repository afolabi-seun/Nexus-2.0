using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.PriorityLevels;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Repositories.PriorityLevels;

public class PriorityLevelRepository : IPriorityLevelRepository
{
    private readonly UtilityDbContext _context;

    public PriorityLevelRepository(UtilityDbContext context) => _context = context;

    public async Task<PriorityLevel?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.PriorityLevels.FirstOrDefaultAsync(e => e.Name == name, ct);

    public async Task<PriorityLevel> AddAsync(PriorityLevel priorityLevel, CancellationToken ct = default)
    {
        _context.PriorityLevels.Add(priorityLevel);
        await _context.SaveChangesAsync(ct);
        return priorityLevel;
    }

    public async Task<IEnumerable<PriorityLevel>> ListAsync(CancellationToken ct = default)
        => await _context.PriorityLevels.AsNoTracking().OrderBy(e => e.SortOrder).ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<PriorityLevel> levels, CancellationToken ct = default)
    {
        _context.PriorityLevels.AddRange(levels);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken ct = default)
        => await _context.PriorityLevels.AnyAsync(e => e.Name == name, ct);
}
