namespace SecurityService.Domain.Interfaces.Services.Otp;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(string identity, CancellationToken ct = default);
    Task<bool> VerifyOtpAsync(string identity, string code, CancellationToken ct = default);
}
