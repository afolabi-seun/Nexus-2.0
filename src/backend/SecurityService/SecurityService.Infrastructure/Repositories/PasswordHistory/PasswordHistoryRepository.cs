using Microsoft.EntityFrameworkCore;
using SecurityService.Domain.Interfaces.Repositories.PasswordHistory;
using SecurityService.Infrastructure.Data;
using SecurityService.Infrastructure.Repositories.Generics;
using PasswordHistoryEntity = SecurityService.Domain.Entities.PasswordHistory;

namespace SecurityService.Infrastructure.Repositories.PasswordHistory;

public class PasswordHistoryRepository : GenericRepository<PasswordHistoryEntity>, IPasswordHistoryRepository
{
    private readonly SecurityDbContext _db;

    public PasswordHistoryRepository(SecurityDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PasswordHistoryEntity>> GetLastNByUserIdAsync(Guid userId, int count, CancellationToken ct = default)
    {
        return await _db.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.DateCreated)
            .Take(count)
            .ToListAsync(ct);
    }
}
