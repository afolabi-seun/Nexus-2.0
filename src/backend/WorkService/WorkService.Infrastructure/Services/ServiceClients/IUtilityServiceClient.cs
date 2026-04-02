using WorkService.Application.Contracts;

namespace WorkService.Infrastructure.Services.ServiceClients;

public interface IUtilityServiceClient
{
    Task<ErrorCodeResponse> GetErrorCodeAsync(string errorCode, CancellationToken ct = default);
}
