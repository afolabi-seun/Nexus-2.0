namespace WorkService.Infrastructure.Configuration;

public class AppSettings
{
    public string DatabaseConnectionString { get; set; } = string.Empty;
    public string RedisConnectionString { get; set; } = string.Empty;
    public string JwtSecretKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public string ProfileServiceBaseUrl { get; set; } = string.Empty;
    public string SecurityServiceBaseUrl { get; set; } = string.Empty;
    public string UtilityServiceBaseUrl { get; set; } = string.Empty;
    public string FrontendUrl { get; set; } = string.Empty;
    public string[] AllowedOrigins { get; set; } = [];
    public string? SeqUrl { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceSecret { get; set; } = string.Empty;
    public string? DatabaseSchema { get; set; }

    public static AppSettings FromEnvironment()
    {
        return new AppSettings
        {
            DatabaseConnectionString = GetRequired("DATABASE_CONNECTION_STRING"),
            RedisConnectionString = GetRequired("REDIS_CONNECTION_STRING"),
            JwtSecretKey = GetRequired("JWT_SECRET_KEY"),
            JwtIssuer = GetRequired("JWT_ISSUER"),
            JwtAudience = GetRequired("JWT_AUDIENCE"),
            ProfileServiceBaseUrl = GetRequired("PROFILE_SERVICE_BASE_URL"),
            SecurityServiceBaseUrl = GetRequired("SECURITY_SERVICE_BASE_URL"),
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
    }

    private static string GetRequired(string key)
        => Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException($"Required environment variable '{key}' is not set.");
}
