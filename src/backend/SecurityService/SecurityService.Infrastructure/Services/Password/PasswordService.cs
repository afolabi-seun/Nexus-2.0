using System.Text.Json;
using System.Text.RegularExpressions;
using SecurityService.Domain.Entities;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Helpers;
using SecurityService.Domain.Interfaces.Repositories.PasswordHistory;
using SecurityService.Domain.Interfaces.Services.Otp;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Domain.Interfaces.Services.Password;
using SecurityService.Infrastructure.Configuration;
using SecurityService.Infrastructure.Data;
using SecurityService.Infrastructure.Services.ServiceClients;
using SecurityService.Infrastructure.Redis;

namespace SecurityService.Infrastructure.Services.Password;

public class PasswordService : IPasswordService
{
    private readonly IPasswordHistoryRepository _passwordHistoryRepository;
    private readonly SecurityDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IOutboxService _outboxService;
    private readonly IProfileServiceClient _profileServiceClient;
    private readonly AppSettings _appSettings;

    private const int PasswordHistoryCount = 5;

    public PasswordService(
        IPasswordHistoryRepository passwordHistoryRepository,
        SecurityDbContext dbContext,
        IOtpService otpService,
        IOutboxService outboxService,
        IProfileServiceClient profileServiceClient,
        AppSettings appSettings)
    {
        _passwordHistoryRepository = passwordHistoryRepository;
        _dbContext = dbContext;
        _otpService = otpService;
        _outboxService = outboxService;
        _profileServiceClient = profileServiceClient;
        _appSettings = appSettings;
    }

    public bool ValidateComplexity(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            throw new PasswordComplexityFailedException("Password must be at least 8 characters long.");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            throw new PasswordComplexityFailedException("Password must contain at least one uppercase letter.");

        if (!Regex.IsMatch(password, @"[a-z]"))
            throw new PasswordComplexityFailedException("Password must contain at least one lowercase letter.");

        if (!Regex.IsMatch(password, @"\d"))
            throw new PasswordComplexityFailedException("Password must contain at least one digit.");

        if (!Regex.IsMatch(password, @"[!@#$%^&*]"))
            throw new PasswordComplexityFailedException("Password must contain at least one special character (!@#$%^&*).");

        return true;
    }

    public async Task<bool> IsPasswordInHistoryAsync(Guid userId, string newPassword, CancellationToken ct = default)
    {
        var recentPasswords = await _passwordHistoryRepository.GetLastNByUserIdAsync(userId, PasswordHistoryCount, ct);

        foreach (var entry in recentPasswords)
        {
            if (BCrypt.Net.BCrypt.Verify(newPassword, entry.PasswordHash))
                return true;
        }

        return false;
    }

    public async Task ForcedChangeAsync(Guid userId, string currentPasswordHash, string newPassword,
        CancellationToken ct = default)
    {
        ValidateComplexity(newPassword);

        // Check new password is not the same as current (temp) password
        if (BCrypt.Net.BCrypt.Verify(newPassword, currentPasswordHash))
            throw new PasswordReuseNotAllowedException();

        // Check password history
        if (await IsPasswordInHistoryAsync(userId, newPassword, ct))
            throw new PasswordRecentlyUsedException();

        // Record old hash in password history
        await _passwordHistoryRepository.AddAsync(new PasswordHistory
        {
            UserId = userId,
            PasswordHash = currentPasswordHash,
            DateCreated = DateTime.UtcNow
        }, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task ResetRequestAsync(string email, CancellationToken ct = default)
    {
        var otpCode = await _otpService.GenerateOtpAsync(email, ct);

        var message = JsonSerializer.Serialize(new
        {
            MessageId = Guid.NewGuid(),
            MessageType = "NotificationRequest",
            ServiceName = "SecurityService",
            Action = "PasswordResetOtp",
            EntityType = "Password",
            EntityId = email,
            NewValue = otpCode,
            Timestamp = DateTime.UtcNow
        });

        await _outboxService.PublishAsync(RedisKeys.Outbox, message, ct);
    }

    public async Task ResetConfirmAsync(string email, string otpCode, string newPassword,
        CancellationToken ct = default)
    {
        await _otpService.VerifyOtpAsync(email, otpCode, ct);

        ValidateComplexity(newPassword);
    }

    public async Task ForcedChangePlatformAdminAsync(Guid platformAdminId, string currentPasswordHash,
        string newPassword, CancellationToken ct = default)
    {
        ValidateComplexity(newPassword);

        if (BCrypt.Net.BCrypt.Verify(newPassword, currentPasswordHash))
            throw new PasswordReuseNotAllowedException();

        if (await IsPasswordInHistoryAsync(platformAdminId, newPassword, ct))
            throw new PasswordRecentlyUsedException();

        await _passwordHistoryRepository.AddAsync(new PasswordHistory
        {
            UserId = platformAdminId,
            PasswordHash = currentPasswordHash,
            DateCreated = DateTime.UtcNow
        }, ct);
        await _dbContext.SaveChangesAsync(ct);

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _profileServiceClient.UpdatePlatformAdminPasswordAsync(platformAdminId, newHash, ct);
    }

    public async Task ResetConfirmPlatformAdminAsync(Guid platformAdminId, string email,
        string otpCode, string newPassword, CancellationToken ct = default)
    {
        await _otpService.VerifyOtpAsync(email, otpCode, ct);

        ValidateComplexity(newPassword);

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _profileServiceClient.UpdatePlatformAdminPasswordAsync(platformAdminId, newHash, ct);
    }
}
