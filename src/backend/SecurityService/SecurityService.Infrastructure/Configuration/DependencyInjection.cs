using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using SecurityService.Domain.Interfaces.Repositories.PasswordHistory;
using SecurityService.Domain.Interfaces.Repositories.ServiceTokens;
using SecurityService.Domain.Interfaces.Services.AnomalyDetection;
using SecurityService.Domain.Interfaces.Services.Auth;
using SecurityService.Domain.Interfaces.Services.ErrorCodeResolver;
using SecurityService.Domain.Interfaces.Services.Jwt;
using SecurityService.Domain.Interfaces.Services.Otp;
using SecurityService.Domain.Interfaces.Services.Outbox;
using SecurityService.Domain.Interfaces.Services.Password;
using SecurityService.Domain.Interfaces.Services.RateLimiter;
using SecurityService.Domain.Interfaces.Services.ServiceToken;
using SecurityService.Domain.Interfaces.Services.Session;
using SecurityService.Infrastructure.Data;
using SecurityService.Infrastructure.Repositories.PasswordHistory;
using SecurityService.Infrastructure.Repositories.ServiceTokens;
using SecurityService.Infrastructure.Services.AnomalyDetection;
using SecurityService.Infrastructure.Services.Auth;
using SecurityService.Infrastructure.Services.ErrorCodeResolver;
using SecurityService.Infrastructure.Services.Jwt;
using SecurityService.Infrastructure.Services.Otp;
using SecurityService.Infrastructure.Services.Outbox;
using SecurityService.Infrastructure.Services.Password;
using SecurityService.Infrastructure.Services.RateLimiter;
using SecurityService.Infrastructure.Services.ServiceClients;
using SecurityService.Infrastructure.Services.Session;
using StackExchange.Redis;
using Microsoft.AspNetCore.Http;

namespace SecurityService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, AppSettings appSettings)
    {
        // Database
        services.AddDbContext<SecurityDbContext>(options =>
            options.UseNpgsql(appSettings.DatabaseConnectionString));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(appSettings.RedisConnectionString));

        // Configuration
        services.AddSingleton(appSettings);
        services.AddSingleton(new JwtConfig
        {
            Issuer = appSettings.JwtIssuer,
            Audience = appSettings.JwtAudience,
            SecretKey = appSettings.JwtSecretKey,
            AccessTokenExpiryMinutes = appSettings.AccessTokenExpiryMinutes,
            RefreshTokenExpiryDays = appSettings.RefreshTokenExpiryDays
        });

        // Repositories
        services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();
        services.AddScoped<IServiceTokenRepository, ServiceTokenRepository>();

        // Domain services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IRateLimiterService, RateLimiterService>();
        services.AddScoped<IAnomalyDetectionService, AnomalyDetectionService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IServiceTokenService, Services.ServiceToken.ServiceTokenService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IErrorCodeResolverService, ErrorCodeResolverService>();
        services.AddScoped<IAuthService, AuthService>();

        // Infrastructure service clients
        services.AddScoped<IProfileServiceClient, ProfileServiceClient>();
        services.AddScoped<IUtilityServiceClient, UtilityServiceClient>();

        // HTTP context accessor
        services.AddHttpContextAccessor();

        // Delegating handler
        services.AddTransient<CorrelationIdDelegatingHandler>();

        // Typed HTTP clients with Polly
        services.AddHttpClient("ProfileService", client =>
            {
                client.BaseAddress = new Uri(appSettings.ProfileServiceBaseUrl);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1))))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));

        services.AddHttpClient("UtilityService", client =>
            {
                client.BaseAddress = new Uri(appSettings.UtilityServiceBaseUrl);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1))))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));

        services.AddHostedService<Services.BackgroundServices.ErrorCodeValidationHostedService>();

        return services;
    }
}
