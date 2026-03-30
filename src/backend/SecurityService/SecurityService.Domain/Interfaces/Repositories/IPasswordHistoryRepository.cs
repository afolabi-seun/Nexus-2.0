using SecurityService.Domain.Entities;

namespace SecurityService.Domain.Interfaces.Repositories;

public interface IPasswordHistoryRepository
{
    Task<IEnumerable<PasswordHistory>> GetLastNByUserIdAsync(Guid userId, int count, CancellationToken ct = default);
    Task AddAsync(PasswordHistory entry, CancellationToken ct = default);
}
