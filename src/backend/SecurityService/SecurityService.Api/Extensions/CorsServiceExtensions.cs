using SecurityService.Infrastructure.Configuration;

namespace SecurityService.Api.Extensions;

/// <summary>
/// Configures CORS policy for the Nexus platform.
/// </summary>
public static class CorsServiceExtensions
{
    public static IServiceCollection AddNexusCors(this IServiceCollection services, AppSettings appSettings)
    {
        var corsOrigins = new List<string>();
        if (!string.IsNullOrWhiteSpace(appSettings.FrontendUrl))
            corsOrigins.Add(appSettings.FrontendUrl);
        corsOrigins.AddRange(appSettings.AllowedOrigins);
        var distinctOrigins = corsOrigins.Where(o => !string.IsNullOrWhiteSpace(o)).Distinct().ToArray();

        services.AddCors(options =>
        {
            options.AddPolicy("NexusPolicy", policy =>
            {
                if (distinctOrigins.Length > 0)
                {
                    policy.WithOrigins(distinctOrigins)
                          .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                          .WithHeaders("Content-Type", "Authorization", "X-Correlation-Id")
                          .WithExposedHeaders("X-Correlation-Id")
                          .AllowCredentials();
                }
                else
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });

        return services;
    }
}
