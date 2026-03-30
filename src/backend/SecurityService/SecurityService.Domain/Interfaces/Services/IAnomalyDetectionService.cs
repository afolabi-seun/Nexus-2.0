namespace SecurityService.Domain.Interfaces.Services;

public interface IAnomalyDetectionService
{
    Task<bool> CheckLoginAnomalyAsync(Guid userId, string ipAddress, CancellationToken ct = default);
    Task AddTrustedIpAsync(Guid userId, string ipAddress, CancellationToken ct = default);
}
