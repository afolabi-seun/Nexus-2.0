using ProfileService.Domain.Results;

namespace ProfileService.Domain.Interfaces.Services.Organizations;

public interface IOrganizationService
{
    Task<ServiceResult<object>> CreateAsync(object request, CancellationToken ct = default);
    Task<ServiceResult<object>> GetByIdAsync(Guid organizationId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateStatusAsync(Guid organizationId, string newStatus, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateSettingsAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<object>> ProvisionAdminAsync(Guid organizationId, object request, CancellationToken ct = default);
}
