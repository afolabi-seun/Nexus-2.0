using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecurityService.Application.Contracts;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Helpers;
using SecurityService.Domain.Interfaces.Services.AnomalyDetection;
using SecurityService.Domain.Interfaces.Services.Auth;
using SecurityService.Domain.Interfaces.Services.Jwt;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Domain.Interfaces.Services.Password;
using SecurityService.Domain.Interfaces.Services.RateLimiter;
using SecurityService.Domain.Interfaces.Services.Session;
using SecurityService.Infrastructure.Configuration;
using SecurityService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace SecurityService.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly ISessionService _sessionService;
    private readonly IRateLimiterService _rateLimiterService;
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly IPasswordService _passwordService;
    private readonly IOutboxService _outboxService;
    private readonly IProfileServiceClient _profileServiceClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IJwtService jwtService,
        ISessionService sessionService,
        IRateLimiterService rateLimiterService,
        IAnomalyDetectionService anomalyDetectionService,
        IPasswordService passwordService,
        IOutboxService outboxService,
        IProfileServiceClient profileServiceClient,
        IConnectionMultiplexer redis,
        AppSettings appSettings,
        ILogger<AuthService> logger)
    {
        _jwtService = jwtService;
        _sessionService = sessionService;
        _rateLimiterService = rateLimiterService;
        _anomalyDetectionService = anomalyDetectionService;
        _passwordService = passwordService;
        _outboxService = outboxService;
        _profileServiceClient = profileServiceClient;
        _redis = redis;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string ipAddress,
        string deviceId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // 1. Check lockout
        var lockedKey = $"lockout:locked:{email}";
        var isLocked = await db.KeyExistsAsync(lockedKey);
        if (isLocked)
            throw new AccountLockedException();

        // 2. Try to resolve user as TeamMember first, then fall back to PlatformAdmin
        ProfileUserResponse? user = null;
        PlatformAdminResponse? platformAdmin = null;

        try
        {
            user = await _profileServiceClient.GetTeamMemberByEmailAsync(email, ct);
        }
        catch (DomainException ex) when (ex.ErrorCode == "NOT_FOUND" || ex.ErrorCode == "MEMBER_NOT_FOUND")
        {
            // TeamMember not found — try PlatformAdmin by username
            try
            {
                platformAdmin = await _profileServiceClient.GetPlatformAdminByUsernameAsync(email, ct);
            }
            catch (DomainException)
            {
                // Neither TeamMember nor PlatformAdmin found
                throw new InvalidCredentialsException();
            }
        }

        if (platformAdmin is not null)
        {
            return await LoginAsPlatformAdminAsync(platformAdmin, password, ipAddress, deviceId, email, db, ct);
        }

        // 3. Check FlgStatus
        if (user!.FlgStatus == EntityStatuses.Suspended || user.FlgStatus == EntityStatuses.Deactivated)
            throw new AccountInactiveException();

        // 4. BCrypt verify password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            // Increment lockout counter
            var lockoutKey = $"lockout:{email}";
            var attempts = await db.StringIncrementAsync(lockoutKey);

            if (attempts == 1)
            {
                await db.KeyExpireAsync(lockoutKey, TimeSpan.FromHours(_appSettings.AccountLockoutWindowHours));
            }

            if (attempts >= _appSettings.AccountLockoutMaxAttempts)
            {
                // Lock the account
                await db.StringSetAsync(lockedKey, "1",
                    TimeSpan.FromMinutes(_appSettings.AccountLockoutDurationMinutes));
                await db.KeyDeleteAsync(lockoutKey);

                // Publish audit event
                await PublishAuditEventAsync("AccountLocked", "Account", email,
                    userId: user.TeamMemberId, organizationId: user.OrganizationId,
                    ipAddress: ipAddress, ct: ct);
            }

            throw new InvalidCredentialsException();
        }

        // 5. Anomaly detection
        await _anomalyDetectionService.CheckLoginAnomalyAsync(user.TeamMemberId, ipAddress, ct);

        // 6. Reset lockout on success
        await db.KeyDeleteAsync($"lockout:{email}");
        await _anomalyDetectionService.AddTrustedIpAsync(user.TeamMemberId, ipAddress, ct);

        // 7. Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(
            user.TeamMemberId, user.OrganizationId, user.PrimaryDepartmentId,
            user.RoleName, user.DepartmentRole ?? string.Empty, deviceId);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var jti = _jwtService.GetJti(accessToken);
        var tokenExpiry = _jwtService.GetTokenExpiry(accessToken);

        // 8. Create session
        await _sessionService.CreateSessionAsync(user.TeamMemberId, deviceId, ipAddress, jti, tokenExpiry, ct);

        // 9. Store BCrypt-hashed refresh token
        var refreshHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        var refreshKey = $"refresh:{user.TeamMemberId}:{deviceId}";
        await db.StringSetAsync(refreshKey, refreshHash,
            TimeSpan.FromDays(_appSettings.RefreshTokenExpiryDays));

        // 10. Publish audit event
        await PublishAuditEventAsync("Login", "Session", $"{user.TeamMemberId}:{deviceId}",
            userId: user.TeamMemberId, organizationId: user.OrganizationId,
            ipAddress: ipAddress, ct: ct);

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _appSettings.AccessTokenExpiryMinutes * 60,
            IsFirstTimeUser = user.IsFirstTimeUser
        };
    }

    private async Task<AuthResult> LoginAsPlatformAdminAsync(
        PlatformAdminResponse platformAdmin, string password, string ipAddress,
        string deviceId, string loginIdentifier, IDatabase db, CancellationToken ct)
    {
        // Check FlgStatus
        if (platformAdmin.FlgStatus == EntityStatuses.Suspended || platformAdmin.FlgStatus == EntityStatuses.Deactivated)
            throw new AccountInactiveException();

        // BCrypt verify password
        if (!BCrypt.Net.BCrypt.Verify(password, platformAdmin.PasswordHash))
        {
            var lockoutKey = $"lockout:{loginIdentifier}";
            var attempts = await db.StringIncrementAsync(lockoutKey);

            if (attempts == 1)
            {
                await db.KeyExpireAsync(lockoutKey, TimeSpan.FromHours(_appSettings.AccountLockoutWindowHours));
            }

            if (attempts >= _appSettings.AccountLockoutMaxAttempts)
            {
                var lockedKey = $"lockout:locked:{loginIdentifier}";
                await db.StringSetAsync(lockedKey, "1",
                    TimeSpan.FromMinutes(_appSettings.AccountLockoutDurationMinutes));
                await db.KeyDeleteAsync(lockoutKey);

                await PublishAuditEventAsync("AccountLocked", "Account", loginIdentifier,
                    userId: platformAdmin.PlatformAdminId, ipAddress: ipAddress, ct: ct);
            }

            throw new InvalidCredentialsException();
        }

        // Reset lockout on success
        await db.KeyDeleteAsync($"lockout:{loginIdentifier}");

        // Generate tokens — PlatformAdmin has no organizationId or departmentId
        var accessToken = _jwtService.GenerateAccessToken(
            platformAdmin.PlatformAdminId, Guid.Empty, Guid.Empty,
            RoleNames.PlatformAdmin, string.Empty, deviceId);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var jti = _jwtService.GetJti(accessToken);
        var tokenExpiry = _jwtService.GetTokenExpiry(accessToken);

        // Create session
        await _sessionService.CreateSessionAsync(platformAdmin.PlatformAdminId, deviceId, ipAddress, jti, tokenExpiry, ct);

        // Store BCrypt-hashed refresh token
        var refreshHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        var refreshKey = $"refresh:{platformAdmin.PlatformAdminId}:{deviceId}";
        await db.StringSetAsync(refreshKey, refreshHash,
            TimeSpan.FromDays(_appSettings.RefreshTokenExpiryDays));

        // Publish audit event
        await PublishAuditEventAsync("Login", "Session", $"{platformAdmin.PlatformAdminId}:{deviceId}",
            userId: platformAdmin.PlatformAdminId, ipAddress: ipAddress, ct: ct);

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _appSettings.AccessTokenExpiryMinutes * 60,
            IsFirstTimeUser = platformAdmin.IsFirstTimeUser
        };
    }

    public async Task LogoutAsync(Guid userId, string deviceId, string jti, DateTime tokenExpiry,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // Remove session
        await _sessionService.RevokeSessionAsync(userId, $"{userId}:{deviceId}", ct);

        // Blacklist jti
        var remainingTtl = tokenExpiry - DateTime.UtcNow;
        if (remainingTtl > TimeSpan.Zero)
        {
            await db.StringSetAsync($"blacklist:{jti}", "1", remainingTtl);
        }

        // Remove refresh token
        await db.KeyDeleteAsync($"refresh:{userId}:{deviceId}");

        // Publish audit event
        await PublishAuditEventAsync("Logout", "Session", $"{userId}:{deviceId}",
            userId: userId, ct: ct);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string deviceId,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // We need to find the user by scanning refresh keys for this device
        // The refresh token is stored at refresh:{userId}:{deviceId}
        // Since we don't have userId from the refresh token itself, we need to scan
        var server = _redis.GetServers().First();
        string? matchedKey = null;
        Guid userId = Guid.Empty;

        await foreach (var key in server.KeysAsync(pattern: $"refresh:*:{deviceId}"))
        {
            var storedHash = await db.StringGetAsync(key);
            if (storedHash.IsNullOrEmpty) continue;

            if (BCrypt.Net.BCrypt.Verify(refreshToken, storedHash!))
            {
                matchedKey = key.ToString();
                // Extract userId from key: refresh:{userId}:{deviceId}
                var parts = matchedKey.Split(':');
                if (parts.Length >= 2 && Guid.TryParse(parts[1], out var parsedId))
                {
                    userId = parsedId;
                }
                break;
            }
        }

        if (matchedKey is null || userId == Guid.Empty)
        {
            // Could be reuse detection — if no key found, the token was already rotated
            // Check if any session exists for this device
            var reusePattern = $"refresh:*:{deviceId}";
            var hasAnyKey = false;
            await foreach (var key in server.KeysAsync(pattern: reusePattern))
            {
                hasAnyKey = true;
                // Extract userId for revocation
                var parts = key.ToString().Split(':');
                if (parts.Length >= 2 && Guid.TryParse(parts[1], out var reuseUserId))
                {
                    // Revoke all sessions for this user (reuse detected)
                    await _sessionService.RevokeAllSessionsAsync(reuseUserId, ct);

                    await PublishAuditEventAsync("RefreshTokenReuse", "Session", deviceId,
                        userId: reuseUserId, ct: ct);
                }
                break;
            }

            if (hasAnyKey)
                throw new RefreshTokenReuseException();

            throw new SessionExpiredException();
        }

        // Invalidate old refresh token
        await db.KeyDeleteAsync(matchedKey);

        // Issue new token pair — we need user info
        var user = await _profileServiceClient.GetTeamMemberByEmailAsync(
            (await GetEmailByUserIdAsync(db, userId, ct))!, ct);

        var accessToken = _jwtService.GenerateAccessToken(
            user.TeamMemberId, user.OrganizationId, user.PrimaryDepartmentId,
            user.RoleName, user.DepartmentRole ?? string.Empty, deviceId);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var jti = _jwtService.GetJti(accessToken);
        var tokenExpiry = _jwtService.GetTokenExpiry(accessToken);

        // Update session
        await _sessionService.CreateSessionAsync(user.TeamMemberId, deviceId, string.Empty, jti, tokenExpiry, ct);

        // Store new refresh hash
        var newRefreshHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
        await db.StringSetAsync($"refresh:{userId}:{deviceId}", newRefreshHash,
            TimeSpan.FromDays(_appSettings.RefreshTokenExpiryDays));

        return new AuthResult
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = _appSettings.AccessTokenExpiryMinutes * 60,
            IsFirstTimeUser = user.IsFirstTimeUser
        };
    }

    private async Task<string?> GetEmailByUserIdAsync(IDatabase db, Guid userId, CancellationToken ct)
    {
        // Try to find email from cached user data
        var server = _redis.GetServers().First();
        await foreach (var key in server.KeysAsync(pattern: "user_cache:*"))
        {
            var json = await db.StringGetAsync(key);
            if (json.IsNullOrEmpty) continue;

            try
            {
                var cached = JsonSerializer.Deserialize<CachedUserInfo>(json!,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cached?.TeamMemberId == userId)
                    return cached.Email;
            }
            catch { /* skip malformed entries */ }
        }

        return null;
    }

    private sealed class CachedUserInfo
    {
        public Guid TeamMemberId { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public async Task GenerateCredentialsAsync(Guid memberId, string email, CancellationToken ct = default)
    {
        // Generate random temp password
        var tempPassword = GenerateRandomPassword();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        // Store via ProfileServiceClient
        await _profileServiceClient.UpdatePasswordHashAsync(memberId, passwordHash, ct);
        await _profileServiceClient.SetIsFirstTimeUserAsync(memberId, true, ct);

        // Publish notification with temp password
        await PublishAuditEventAsync("CredentialGenerated", "Account", memberId.ToString(),
            userId: memberId, newValue: tempPassword, ct: ct);
    }

    private static string GenerateRandomPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        var password = new char[12];
        password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (var i = 4; i < password.Length; i++)
        {
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        // Shuffle
        RandomNumberGenerator.Shuffle(password.AsSpan());
        return new string(password);
    }

    private async Task PublishAuditEventAsync(string action, string entityType, string entityId,
        Guid? userId = null, Guid? organizationId = null, string? ipAddress = null,
        string? newValue = null, CancellationToken ct = default)
    {
        var message = JsonSerializer.Serialize(new
        {
            MessageId = Guid.NewGuid(),
            MessageType = "AuditEvent",
            ServiceName = "SecurityService",
            OrganizationId = organizationId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            NewValue = newValue,
            Timestamp = DateTime.UtcNow
        });

        await _outboxService.PublishAsync("outbox:security", message, ct);
    }
}
