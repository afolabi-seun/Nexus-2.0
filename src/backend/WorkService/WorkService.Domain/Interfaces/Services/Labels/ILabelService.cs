using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Labels;

public interface ILabelService
{
    Task<ServiceResult<object>> CreateAsync(Guid organizationId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid organizationId, CancellationToken ct = default);
    Task<ServiceResult<object>> UpdateAsync(Guid labelId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(Guid labelId, CancellationToken ct = default);
}
