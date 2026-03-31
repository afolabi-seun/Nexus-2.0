namespace ProfileService.Domain.Interfaces.Services.Preferences;

public interface IPreferenceService
{
    Task<object> GetAsync(Guid memberId, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid memberId, object request, CancellationToken ct = default);
}
