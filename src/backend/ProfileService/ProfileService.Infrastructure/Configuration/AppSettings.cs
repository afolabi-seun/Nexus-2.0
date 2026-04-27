namespace ProfileService.Infrastructure.Configuration;

public class AppSettings
{
    // Database
    public string DatabaseConnectionString { get; set; } = string.Empty;

    // Redis
    public string RedisConnectionString { get; set; } = string.Empty;

    // JWT (for token validation — tokens issued by SecurityService)
    public string JwtSecretKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;

    // Service URLs
    public string SecurityServiceBaseUrl { get; set; } = string.Empty;
    public string UtilityServiceBaseUrl { get; set; } = string.Empty;

    // CORS
    public string FrontendUrl { get; set; } = string.Empty;
    public string[] AllowedOrigins { get; set; } = [];

    // Serilog
    public string? SeqUrl { get; set; }

    // Service Auth (for inter-service calls)
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceSecret { get; set; } = string.Empty;

    // Schema
    public string? DatabaseSchema { get; set; }

    // Invite
    public int InviteExpiryHours { get; set; } = 48;
    public int InviteTokenLength { get; set; } = 128;

    // Device
    public int MaxDevicesPerUser { get; set; } = 5;

    public static AppSettings FromEnvironment()
    {
        return new AppSettings
        {
            DatabaseConnectionString = GetRequired("DATABASE_CONNECTION_STRING"),
            RedisConnectionString = GetRequired("REDIS_CONNECTION_STRING"),
            JwtSecretKey = GetRequired("JWT_SECRET_KEY"),
            JwtIssuer = GetRequired("JWT_ISSUER"),
            JwtAudience = GetRequired("JWT_AUDIENCE"),
            SecurityServiceBaseUrl = GetRequired("SECURITY_SERVICE_BASE_URL"),
            UtilityServiceBaseUrl = GetRequired("UTILITY_SERVICE_BASE_URL"),
            FrontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173",
            AllowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            SeqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341",
            ServiceId = GetRequired("SERVICE_ID"),
            ServiceName = GetRequired("SERVICE_NAME"),
            ServiceSecret = GetRequired("SERVICE_SECRET"),
            InviteExpiryHours = GetOptionalInt("INVITE_EXPIRY_HOURS", 48),
            InviteTokenLength = GetOptionalInt("INVITE_TOKEN_LENGTH", 128),
            MaxDevicesPerUser = GetOptionalInt("MAX_DEVICES_PER_USER", 5),
            DatabaseSchema = Environment.GetEnvironmentVariable("DATABASE_SCHEMA"),
        };
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
