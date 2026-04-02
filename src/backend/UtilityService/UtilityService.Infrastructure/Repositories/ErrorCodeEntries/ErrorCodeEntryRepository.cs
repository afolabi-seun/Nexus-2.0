using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.ErrorCodeEntries;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Repositories.Generics;

namespace UtilityService.Infrastructure.Repositories.ErrorCodeEntries;

public class ErrorCodeEntryRepository : GenericRepository<ErrorCodeEntry>, IErrorCodeEntryRepository
{
    private readonly UtilityDbContext _db;

    public ErrorCodeEntryRepository(UtilityDbContext db) : base(db) => _db = db;

    public async Task<ErrorCodeEntry?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _db.ErrorCodeEntries.FirstOrDefaultAsync(e => e.Code == code, ct);

    public async Task RemoveAsync(ErrorCodeEntry entry, CancellationToken ct = default)
    {
        _db.ErrorCodeEntries.Remove(entry);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<ErrorCodeEntry>> ListAsync(CancellationToken ct = default)
        => await _db.ErrorCodeEntries.AsNoTracking().OrderBy(e => e.Code).ToListAsync(ct);
}
