using ProfileService.Application.Contracts;

namespace ProfileService.Infrastructure.Services.ServiceClients;

public interface IUtilityServiceClient
{
    Task<ErrorCodeResponse> GetErrorCodeAsync(string errorCode, CancellationToken ct = default);
    Task<Dictionary<string, (string ResponseCode, string ResponseDescription)>> GetAllErrorCodesAsync(CancellationToken ct = default);
}
