using WorkService.Application.Contracts;

namespace WorkService.Infrastructure.Services.ServiceClients;

public interface IUtilityServiceClient
{
    Task<ErrorCodeResponse> GetErrorCodeAsync(string errorCode, CancellationToken ct = default);
    Task DispatchNotificationAsync(Guid organizationId, Guid userId, string recipient, string notificationType, string subject, string channels, Dictionary<string, string>? templateVars = null, CancellationToken ct = default);
}
