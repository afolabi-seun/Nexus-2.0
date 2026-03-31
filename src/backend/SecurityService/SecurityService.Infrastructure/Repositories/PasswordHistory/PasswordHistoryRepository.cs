using Microsoft.EntityFrameworkCore;
using SecurityService.Domain.Entities;
using SecurityService.Domain.Interfaces.Repositories.PasswordHistory;
using SecurityService.Infrastructure.Data;
using PasswordHistoryEntity = SecurityService.Domain.Entities.PasswordHistory;

namespace SecurityService.Infrastructure.Repositories.PasswordHistory;

public class PasswordHistoryRepository : IPasswordHistoryRepository
{
    private readonly SecurityDbContext _context;

    public PasswordHistoryRepository(SecurityDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PasswordHistoryEntity>> GetLastNByUserIdAsync(Guid userId, int count, CancellationToken ct = default)
    {
        return await _context.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.DateCreated)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task AddAsync(PasswordHistoryEntity entry, CancellationToken ct = default)
    {
        await _context.PasswordHistories.AddAsync(entry, ct);
        await _context.SaveChangesAsync(ct);
    }
}
