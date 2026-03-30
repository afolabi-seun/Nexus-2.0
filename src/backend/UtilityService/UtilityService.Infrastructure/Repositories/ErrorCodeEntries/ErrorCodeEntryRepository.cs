using Microsoft.EntityFrameworkCore;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Repositories.ErrorCodeEntries;

public class ErrorCodeEntryRepository : IErrorCodeEntryRepository
{
    private readonly UtilityDbContext _context;

    public ErrorCodeEntryRepository(UtilityDbContext context) => _context = context;

    public async Task<ErrorCodeEntry?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _context.ErrorCodeEntries.FirstOrDefaultAsync(e => e.Code == code, ct);

    public async Task<ErrorCodeEntry> AddAsync(ErrorCodeEntry entry, CancellationToken ct = default)
    {
        _context.ErrorCodeEntries.Add(entry);
        await _context.SaveChangesAsync(ct);
        return entry;
    }

    public async Task UpdateAsync(ErrorCodeEntry entry, CancellationToken ct = default)
    {
        _context.ErrorCodeEntries.Update(entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(ErrorCodeEntry entry, CancellationToken ct = default)
    {
        _context.ErrorCodeEntries.Remove(entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<ErrorCodeEntry>> ListAsync(CancellationToken ct = default)
        => await _context.ErrorCodeEntries.AsNoTracking().OrderBy(e => e.Code).ToListAsync(ct);
}
