namespace SecurityService.Domain.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, string ipAddress, string deviceId, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, string deviceId, string jti, DateTime tokenExpiry, CancellationToken ct = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string deviceId, CancellationToken ct = default);
    Task GenerateCredentialsAsync(Guid memberId, string email, CancellationToken ct = default);
}

public class AuthResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public bool IsFirstTimeUser { get; set; }
}
