using PasswordHistoryEntity = SecurityService.Domain.Entities.PasswordHistory;

namespace SecurityService.Domain.Interfaces.Repositories.PasswordHistory;

public interface IPasswordHistoryRepository
{
    Task<IEnumerable<PasswordHistoryEntity>> GetLastNByUserIdAsync(Guid userId, int count, CancellationToken ct = default);
    Task AddAsync(PasswordHistoryEntity entry, CancellationToken ct = default);
}
