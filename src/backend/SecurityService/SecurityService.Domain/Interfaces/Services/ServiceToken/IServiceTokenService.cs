namespace SecurityService.Domain.Interfaces.Services.ServiceToken;

public interface IServiceTokenService
{
    Task<ServiceTokenResult> IssueTokenAsync(string serviceId, string serviceName, CancellationToken ct = default);
    Task<bool> ValidateServiceTokenAsync(string token, CancellationToken ct = default);
}

public class ServiceTokenResult
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
