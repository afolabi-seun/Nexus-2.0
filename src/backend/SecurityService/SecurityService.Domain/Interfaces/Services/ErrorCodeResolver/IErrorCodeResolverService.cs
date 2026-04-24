namespace SecurityService.Domain.Interfaces.Services.ErrorCodeResolver;

public interface IErrorCodeResolverService
{
    Task<(string ResponseCode, string ResponseDescription)> ResolveAsync(string errorCode, CancellationToken ct = default);
    Task RefreshCacheAsync(CancellationToken ct = default);
    void ClearMemoryCache();
}
