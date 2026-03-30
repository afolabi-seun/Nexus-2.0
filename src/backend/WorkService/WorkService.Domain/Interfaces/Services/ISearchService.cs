namespace WorkService.Domain.Interfaces.Services;

public interface ISearchService
{
    Task<object> SearchAsync(Guid organizationId, object request, CancellationToken ct = default);
}
