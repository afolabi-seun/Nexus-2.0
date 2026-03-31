using UtilityService.Domain.Entities;

namespace UtilityService.Domain.Interfaces.Repositories;

public interface IErrorCodeEntryRepository
{
    Task<ErrorCodeEntry?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<ErrorCodeEntry> AddAsync(ErrorCodeEntry entry, CancellationToken ct = default);
    Task UpdateAsync(ErrorCodeEntry entry, CancellationToken ct = default);
    Task RemoveAsync(ErrorCodeEntry entry, CancellationToken ct = default);
    Task<IEnumerable<ErrorCodeEntry>> ListAsync(CancellationToken ct = default);
}
