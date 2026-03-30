namespace SecurityService.Domain.Interfaces.Services;

public interface IPasswordService
{
    Task ForcedChangeAsync(Guid userId, string currentPasswordHash, string newPassword, CancellationToken ct = default);
    Task ResetRequestAsync(string email, CancellationToken ct = default);
    Task ResetConfirmAsync(string email, string otpCode, string newPassword, CancellationToken ct = default);
    bool ValidateComplexity(string password);
    Task<bool> IsPasswordInHistoryAsync(Guid userId, string newPassword, CancellationToken ct = default);
}
