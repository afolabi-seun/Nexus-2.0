namespace BillingService.Domain.Interfaces.Services.ErrorCodeResolver;

public interface IErrorCodeResolverService
{
    Task<(string responseCode, string responseDescription)> ResolveAsync(string errorCode, CancellationToken ct);
    Task RefreshCacheAsync(CancellationToken ct = default);
    void ClearMemoryCache();
}
