namespace BillingService.Domain.Interfaces.Services;

public interface IErrorCodeResolverService
{
    Task<(string responseCode, string responseDescription)> ResolveAsync(string errorCode, CancellationToken ct);
}
