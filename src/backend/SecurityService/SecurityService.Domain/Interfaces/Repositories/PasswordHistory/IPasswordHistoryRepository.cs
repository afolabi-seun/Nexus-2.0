using SecurityService.Domain.Interfaces.Repositories.Generics;
using PasswordHistoryEntity = SecurityService.Domain.Entities.PasswordHistory;

namespace SecurityService.Domain.Interfaces.Repositories.PasswordHistory;

public interface IPasswordHistoryRepository : IGenericRepository<PasswordHistoryEntity>
{
    Task<IEnumerable<PasswordHistoryEntity>> GetLastNByUserIdAsync(Guid userId, int count, CancellationToken ct = default);
}
