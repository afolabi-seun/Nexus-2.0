namespace SecurityService.Domain.Interfaces.Services;

public interface ISessionService
{
    Task CreateSessionAsync(Guid userId, string deviceId, string ipAddress, string jti, DateTime tokenExpiry, CancellationToken ct = default);
    Task<IEnumerable<SessionInfo>> GetSessionsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task RevokeSessionAsync(Guid userId, string sessionId, CancellationToken ct = default);
    Task RevokeAllSessionsExceptCurrentAsync(Guid userId, string currentDeviceId, CancellationToken ct = default);
    Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct = default);
}

public class SessionInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
