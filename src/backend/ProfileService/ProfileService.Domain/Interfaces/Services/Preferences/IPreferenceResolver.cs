using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.Preferences;

public interface IPreferenceResolver
{
    Task<ServiceResult<object>> ResolveAsync(Guid userId, Guid departmentId, Guid organizationId, CancellationToken ct = default);
}
