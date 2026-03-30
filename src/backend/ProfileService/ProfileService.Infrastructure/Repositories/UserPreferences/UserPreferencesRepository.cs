using Microsoft.EntityFrameworkCore;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Infrastructure.Data;
using UserPreferencesEntity = ProfileService.Domain.Entities.UserPreferences;

namespace ProfileService.Infrastructure.Repositories.UserPreferences;

public class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly ProfileDbContext _context;

    public UserPreferencesRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferencesEntity?> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.UserPreferences.FirstOrDefaultAsync(up => up.TeamMemberId == memberId, ct);
    }

    public async Task<UserPreferencesEntity> AddAsync(UserPreferencesEntity preferences, CancellationToken ct = default)
    {
        await _context.UserPreferences.AddAsync(preferences, ct);
        await _context.SaveChangesAsync(ct);
        return preferences;
    }

    public async Task UpdateAsync(UserPreferencesEntity preferences, CancellationToken ct = default)
    {
        _context.UserPreferences.Update(preferences);
        await _context.SaveChangesAsync(ct);
    }
}
