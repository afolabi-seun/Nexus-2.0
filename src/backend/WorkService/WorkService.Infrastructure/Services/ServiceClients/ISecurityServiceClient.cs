namespace WorkService.Infrastructure.Services.ServiceClients;

public interface ISecurityServiceClient
{
    Task<string> GetServiceTokenAsync(CancellationToken ct = default);
}
