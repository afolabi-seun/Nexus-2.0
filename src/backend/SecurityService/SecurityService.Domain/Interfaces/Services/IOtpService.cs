namespace SecurityService.Domain.Interfaces.Services;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string identity, CancellationToken ct = default);
    Task<bool> VerifyOtpAsync(string identity, string code, CancellationToken ct = default);
}
