using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Repositories.StripeEvents;
using BillingService.Domain.Interfaces.Repositories.Subscriptions;
using BillingService.Domain.Interfaces.Repositories.UsageRecords;
using BillingService.Domain.Interfaces.Services.AdminBilling;
using BillingService.Domain.Interfaces.Services.ErrorCodeResolver;
using BillingService.Domain.Interfaces.Services.FeatureGates;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Domain.Interfaces.Services.Plans;
using BillingService.Domain.Interfaces.Services.Stripe;
using BillingService.Domain.Interfaces.Services.Subscriptions;
using BillingService.Domain.Interfaces.Services.Usage;
using BillingService.Infrastructure.Data;
using BillingService.Infrastructure.Repositories.Subscriptions;
using BillingService.Infrastructure.Repositories.Plans;
using BillingService.Infrastructure.Repositories.UsageRecords;
using BillingService.Infrastructure.Repositories.StripeEvents;
using BillingService.Infrastructure.Services.BackgroundServices;
using BillingService.Infrastructure.Services.ErrorCodeResolver;
using BillingService.Infrastructure.Services.FeatureGates;
using BillingService.Infrastructure.Services.Outbox;
using BillingService.Infrastructure.Services.Plans;
using BillingService.Infrastructure.Services.ServiceClients;
using BillingService.Infrastructure.Services.Stripe;
using BillingService.Infrastructure.Services.AdminBilling;
using BillingService.Infrastructure.Services.Subscriptions;
using BillingService.Infrastructure.Services.Usage;
using StackExchange.Redis;

namespace BillingService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, AppSettings appSettings)
    {
        // Database
        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(appSettings.DatabaseConnectionString));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(appSettings.RedisConnectionString));

        // Configuration
        services.AddSingleton(appSettings);

        // Repositories
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IUsageRecordRepository, UsageRecordRepository>();
        services.AddScoped<IStripeEventRepository, StripeEventRepository>();

        // Domain services
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IFeatureGateService, FeatureGateService>();
        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<IStripePaymentService, StripePaymentService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IErrorCodeResolverService, ErrorCodeResolverService>();
        services.AddScoped<StripeWebhookService>();

        // Admin services
        services.AddScoped<IAdminBillingService, AdminBillingService>();
        services.AddScoped<IAdminPlanService, AdminPlanService>();

        // Service clients
        services.AddScoped<IProfileServiceClient, ProfileServiceClient>();
        services.AddScoped<ISecurityServiceClient, SecurityServiceClient>();
        services.AddScoped<IUtilityServiceClient, UtilityServiceClient>();

        // HTTP context accessor
        services.AddHttpContextAccessor();

        // Delegating handler
        services.AddTransient<CorrelationIdDelegatingHandler>();

        // Typed HTTP clients with Polly
        services.AddHttpClient("SecurityService", client =>
            {
                client.BaseAddress = new Uri(appSettings.SecurityServiceBaseUrl);
            })
            .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
            .AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1))))
            .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));

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

        // Background services
        services.AddHostedService<TrialExpiryHostedService>();
        services.AddHostedService<UsagePersistenceHostedService>();

        return services;
    }
}
