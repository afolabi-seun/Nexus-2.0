namespace WorkService.Domain.Interfaces.Services.Search;

public interface ISearchService
{
    Task<object> SearchAsync(Guid organizationId, object request, CancellationToken ct = default);
}
