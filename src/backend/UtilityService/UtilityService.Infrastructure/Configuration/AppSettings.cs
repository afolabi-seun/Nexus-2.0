namespace UtilityService.Infrastructure.Configuration;

public class AppSettings
{
    public string DatabaseConnectionString { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public string JwtSecretKey { get; set; } = string.Empty;
    public string RedisConnectionString { get; set; } = string.Empty;
    public string FrontendUrl { get; set; } = string.Empty;
    public string[] AllowedOrigins { get; set; } = [];
    public string? SeqUrl { get; set; }

    // Outbox settings
    public int OutboxPollIntervalSeconds { get; set; } = 30;

    // Retention settings
    public int RetentionPeriodDays { get; set; } = 90;
    public int RetentionScheduleHour { get; set; } = 2;

    // Notification settings
    public int NotificationRetryMax { get; set; } = 3;

    // SMTP settings
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public string SmtpFromAddress { get; set; } = "noreply@nexus-platform.local";
    public string SmtpFromName { get; set; } = "Nexus Platform";
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; } = false;

    public static AppSettings FromEnvironment()
    {
        return new AppSettings
        {
            DatabaseConnectionString = GetRequired("DATABASE_URL"),
            RedisConnectionString = GetRequired("REDIS_URL"),
            JwtSecretKey = GetRequired("JWT_SECRET"),
            JwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "Nexus-2.0",
            JwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "Nexus-2.0",
            FrontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173",
            AllowedOrigins = (Environment.GetEnvironmentVariable("ALLOWED_ORIGINS") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            SeqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341",
            OutboxPollIntervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("OUTBOX_POLL_INTERVAL_SECONDS"), out var poll) ? poll : 30,
            RetentionPeriodDays = int.TryParse(Environment.GetEnvironmentVariable("RETENTION_PERIOD_DAYS"), out var ret) ? ret : 90,
            RetentionScheduleHour = int.TryParse(Environment.GetEnvironmentVariable("RETENTION_SCHEDULE_HOUR"), out var hour) ? hour : 2,
            NotificationRetryMax = int.TryParse(Environment.GetEnvironmentVariable("NOTIFICATION_RETRY_MAX"), out var retry) ? retry : 3,
            SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "localhost",
            SmtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var smtpPort) ? smtpPort : 1025,
            SmtpFromAddress = Environment.GetEnvironmentVariable("SMTP_FROM_ADDRESS") ?? "noreply@nexus-platform.local",
            SmtpFromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "Nexus Platform",
            SmtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME"),
            SmtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
            SmtpUseSsl = bool.TryParse(Environment.GetEnvironmentVariable("SMTP_USE_SSL"), out var ssl) && ssl,
        };
    }

    private static string GetRequired(string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException($"Required environment variable '{key}' is not set.");
    }
}
