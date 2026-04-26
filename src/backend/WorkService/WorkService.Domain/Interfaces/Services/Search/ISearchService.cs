using WorkService.Domain.Results;

namespace WorkService.Domain.Interfaces.Services.Search;

public interface ISearchService
{
    Task<ServiceResult<object>> SearchAsync(Guid organizationId, object request, CancellationToken ct = default);
}
