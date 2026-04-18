using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecurityService.Domain.Exceptions;
using SecurityService.Domain.Interfaces.Services.Otp;
using SecurityService.Infrastructure.Configuration;
using StackExchange.Redis;
using SecurityService.Infrastructure.Redis;

namespace SecurityService.Infrastructure.Services.Otp;

public class OtpService : IOtpService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;
    private readonly ILogger<OtpService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OtpService(IConnectionMultiplexer redis, AppSettings appSettings, ILogger<OtpService> logger)
    {
        _redis = redis;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<string> GenerateOtpAsync(string identity, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Otp(identity);

        var code = GenerateSecureCode();
        var otpData = new OtpData { Code = code, Attempts = 0 };
        var json = JsonSerializer.Serialize(otpData, JsonOptions);
        var ttl = TimeSpan.FromMinutes(_appSettings.OtpExpiryMinutes);

        await db.StringSetAsync(key, json, ttl);
        _logger.LogInformation("OTP generated for identity {Identity}", identity);

        return code;
    }

    public async Task<bool> VerifyOtpAsync(string identity, string code, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.Otp(identity);

        var json = await db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
        {
            throw new OtpExpiredException();
        }

        var otpData = JsonSerializer.Deserialize<OtpData>(json!, JsonOptions);
        if (otpData is null)
        {
            throw new OtpExpiredException();
        }

        if (otpData.Code == code)
        {
            await db.KeyDeleteAsync(key);
            _logger.LogInformation("OTP verified successfully for identity {Identity}", identity);
            return true;
        }

        // Increment attempts on failure
        otpData.Attempts++;

        if (otpData.Attempts >= _appSettings.OtpMaxAttempts)
        {
            await db.KeyDeleteAsync(key);
            _logger.LogWarning("OTP max attempts exceeded for identity {Identity}", identity);
            throw new OtpMaxAttemptsException();
        }

        // Update with incremented attempts, preserving remaining TTL
        var remainingTtl = await db.KeyTimeToLiveAsync(key);
        var updatedJson = JsonSerializer.Serialize(otpData, JsonOptions);

        if (remainingTtl.HasValue && remainingTtl.Value > TimeSpan.Zero)
        {
            await db.StringSetAsync(key, updatedJson, remainingTtl.Value);
        }
        else
        {
            await db.KeyDeleteAsync(key);
        }

        _logger.LogWarning("OTP verification failed for identity {Identity}. Attempt {Attempt}/{Max}",
            identity, otpData.Attempts, _appSettings.OtpMaxAttempts);

        throw new OtpVerificationFailedException();
    }

    private static string GenerateSecureCode()
    {
        var randomNumber = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return randomNumber.ToString("D6");
    }

    private sealed class OtpData
    {
        public string Code { get; set; } = string.Empty;
        public int Attempts { get; set; }
    }
}
