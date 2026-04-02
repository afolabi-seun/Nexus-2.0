using UtilityService.Domain.Entities;
using UtilityService.Domain.Interfaces.Repositories.Generics;

namespace UtilityService.Domain.Interfaces.Repositories.ErrorCodeEntries;

public interface IErrorCodeEntryRepository : IGenericRepository<ErrorCodeEntry>
{
    Task<ErrorCodeEntry?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task RemoveAsync(ErrorCodeEntry entry, CancellationToken ct = default);
    Task<IEnumerable<ErrorCodeEntry>> ListAsync(CancellationToken ct = default);
}
