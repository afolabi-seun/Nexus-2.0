namespace SecurityService.Infrastructure.Configuration;

public class AppSettings
{
    // Database
    public string DatabaseConnectionString { get; set; } = string.Empty;

    // Redis
    public string RedisConnectionString { get; set; } = string.Empty;

    // JWT
    public string JwtSecretKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;

    // Rate Limiting
    public int LoginRateLimitMax { get; set; } = 5;
    public int LoginRateLimitWindowMinutes { get; set; } = 15;
    public int OtpRateLimitMax { get; set; } = 3;
    public int OtpRateLimitWindowMinutes { get; set; } = 5;

    // Account Lockout
    public int AccountLockoutMaxAttempts { get; set; } = 10;
    public int AccountLockoutWindowHours { get; set; } = 24;
    public int AccountLockoutDurationMinutes { get; set; } = 60;

    // OTP
    public int OtpExpiryMinutes { get; set; } = 5;
    public int OtpMaxAttempts { get; set; } = 3;

    // Service URLs
    public string ProfileServiceBaseUrl { get; set; } = string.Empty;
    public string UtilityServiceBaseUrl { get; set; } = string.Empty;

    // CORS
    public string FrontendUrl { get; set; } = string.Empty;
    public string[] AllowedOrigins { get; set; } = [];

    // Serilog
    public string? SeqUrl { get; set; }

    // Schema
    public string? DatabaseSchema { get; set; }

    // Service Auth
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceSecret { get; set; } = string.Empty;

    public static AppSettings FromEnvironment()
    {
        var settings = new AppSettings
        {
            DatabaseConnectionString = GetRequired("DATABASE_CONNECTION_STRING"),
            RedisConnectionString = GetRequired("REDIS_CONNECTION_STRING"),
            JwtSecretKey = GetRequired("JWT_SECRET_KEY"),
            JwtIssuer = GetRequired("JWT_ISSUER"),
            JwtAudience = GetRequired("JWT_AUDIENCE"),
            AccessTokenExpiryMinutes = GetOptionalInt("ACCESS_TOKEN_EXPIRY_MINUTES", 15),
            RefreshTokenExpiryDays = GetOptionalInt("REFRESH_TOKEN_EXPIRY_DAYS", 7),
            LoginRateLimitMax = GetOptionalInt("LOGIN_RATE_LIMIT_MAX", 5),
            LoginRateLimitWindowMinutes = GetOptionalInt("LOGIN_RATE_LIMIT_WINDOW_MINUTES", 15),
            OtpRateLimitMax = GetOptionalInt("OTP_RATE_LIMIT_MAX", 3),
            OtpRateLimitWindowMinutes = GetOptionalInt("OTP_RATE_LIMIT_WINDOW_MINUTES", 5),
            AccountLockoutMaxAttempts = GetOptionalInt("ACCOUNT_LOCKOUT_MAX_ATTEMPTS", 10),
            AccountLockoutWindowHours = GetOptionalInt("ACCOUNT_LOCKOUT_WINDOW_HOURS", 24),
            AccountLockoutDurationMinutes = GetOptionalInt("ACCOUNT_LOCKOUT_DURATION_MINUTES", 60),
            OtpExpiryMinutes = GetOptionalInt("OTP_EXPIRY_MINUTES", 5),
            OtpMaxAttempts = GetOptionalInt("OTP_MAX_ATTEMPTS", 3),
            ProfileServiceBaseUrl = GetRequired("PROFILE_SERVICE_BASE_URL"),
            UtilityServiceBaseUrl = GetRequired("UTILITY_SERVICE_BASE_URL"),
            FrontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173",
            AllowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            SeqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341",
            ServiceId = GetRequired("SERVICE_ID"),
            ServiceName = GetRequired("SERVICE_NAME"),
            ServiceSecret = GetRequired("SERVICE_SECRET"),
            DatabaseSchema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA"),
        };

        return settings;
    }

    private static string GetRequired(string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException($"Required environment variable '{key}' is not set.");
    }

    private static int GetOptionalInt(string key, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return value is not null && int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
