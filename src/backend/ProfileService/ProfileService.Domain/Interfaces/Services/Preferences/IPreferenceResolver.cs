namespace ProfileService.Domain.Interfaces.Services.Preferences;

public interface IPreferenceResolver
{
    Task<object> ResolveAsync(Guid userId, Guid departmentId, Guid organizationId, CancellationToken ct = default);
}
