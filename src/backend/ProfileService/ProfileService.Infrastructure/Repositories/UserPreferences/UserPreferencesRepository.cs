using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories.UserPreferenceSettings;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Generics;
using UserPreferencesEntity = ProfileService.Domain.Entities.UserPreferences;

namespace ProfileService.Infrastructure.Repositories.UserPreferences;

public class UserPreferencesRepository : GenericRepository<UserPreferencesEntity>, IUserPreferencesRepository
{
    private readonly ProfileDbContext _db;

    public UserPreferencesRepository(ProfileDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<UserPreferencesEntity?> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _db.UserPreferences.FirstOrDefaultAsync(up => up.TeamMemberId == memberId, ct);
    }
}
