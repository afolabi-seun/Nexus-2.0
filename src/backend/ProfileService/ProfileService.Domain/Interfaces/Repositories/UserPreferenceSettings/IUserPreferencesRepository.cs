using ProfileService.Domain.Entities;

namespace ProfileService.Domain.Interfaces.Repositories.UserPreferenceSettings;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default);
    Task<UserPreferences> AddAsync(UserPreferences preferences, CancellationToken ct = default);
    Task UpdateAsync(UserPreferences preferences, CancellationToken ct = default);
}
