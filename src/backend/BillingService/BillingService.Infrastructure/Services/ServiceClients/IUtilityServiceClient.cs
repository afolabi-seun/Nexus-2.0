using BillingService.Application.Contracts;

namespace BillingService.Infrastructure.Services.ServiceClients;

public interface IUtilityServiceClient
{
    Task<ErrorCodeResponse> GetErrorCodeAsync(string errorCode, CancellationToken ct = default);
}
