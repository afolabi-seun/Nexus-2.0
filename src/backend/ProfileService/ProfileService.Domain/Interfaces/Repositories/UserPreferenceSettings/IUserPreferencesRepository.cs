using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.Generics;

namespace ProfileService.Domain.Interfaces.Repositories.UserPreferenceSettings;

public interface IUserPreferencesRepository : IGenericRepository<UserPreferences>
{
    Task<UserPreferences?> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default);
}
