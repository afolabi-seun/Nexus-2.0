namespace ProfileService.Domain.Interfaces.Services;

public interface IOrganizationService
{
    Task<object> CreateAsync(object request, CancellationToken ct = default);
    Task<object> GetByIdAsync(Guid organizationId, CancellationToken ct = default);
    Task<object> UpdateAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid organizationId, string newStatus, CancellationToken ct = default);
    Task<object> UpdateSettingsAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<object> ListAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<object> ProvisionAdminAsync(Guid organizationId, object request, CancellationToken ct = default);
}
