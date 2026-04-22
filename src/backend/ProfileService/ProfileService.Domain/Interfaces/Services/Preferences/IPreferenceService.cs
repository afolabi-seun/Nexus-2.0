using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.Preferences;

public interface IPreferenceService
{
    Task<ServiceResult<object>> GetAsync(Guid memberId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid memberId, object request, CancellationToken ct = default);
}
