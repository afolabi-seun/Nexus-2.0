using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.SavedFilters;

public interface ISavedFilterService
{
    Task<ServiceResult<object>> CreateAsync(Guid orgId, Guid userId, object request, CancellationToken ct = default);
    Task<ServiceResult<object>> ListAsync(Guid orgId, Guid userId, CancellationToken ct = default);
    Task<ServiceResult<object>> DeleteAsync(Guid filterId, CancellationToken ct = default);
}
