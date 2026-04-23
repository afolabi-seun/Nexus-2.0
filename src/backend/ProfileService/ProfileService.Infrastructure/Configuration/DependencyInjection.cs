using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.Devices;
using ProfileService.Domain.Interfaces.Repositories.Invites;
using ProfileService.Domain.Interfaces.Repositories.NavigationItems;
using ProfileService.Domain.Interfaces.Repositories.NotificationSettings;
using ProfileService.Domain.Interfaces.Repositories.NotificationTypes;
using ProfileService.Domain.Interfaces.Repositories.Organizations;
using ProfileService.Domain.Interfaces.Repositories.PlatformAdmins;
using ProfileService.Domain.Interfaces.Repositories.Roles;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Domain.Interfaces.Repositories.UserPreferenceSettings;
using ProfileService.Domain.Interfaces.Services.Departments;
using ProfileService.Domain.Interfaces.Services.Devices;
using ProfileService.Domain.Interfaces.Services.ErrorCodeResolver;
using ProfileService.Domain.Interfaces.Services.Invites;
using ProfileService.Domain.Interfaces.Services.Navigation;
using ProfileService.Domain.Interfaces.Services.NotificationSettings;
using ProfileService.Domain.Interfaces.Services.Organizations;
using ProfileService.Domain.Interfaces.Services.Outbox;
using ProfileService.Domain.Interfaces.Services.PlatformAdmins;
using ProfileService.Domain.Interfaces.Services.Preferences;
using ProfileService.Domain.Interfaces.Services.Roles;
using ProfileService.Domain.Interfaces.Services.TeamMembers;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Repositories.Organizations;
using ProfileService.Infrastructure.Repositories.Departments;
using ProfileService.Infrastructure.Repositories.DepartmentMembers;
using ProfileService.Infrastructure.Repositories.TeamMembers;
using ProfileService.Infrastructure.Repositories.Roles;
using ProfileService.Infrastructure.Repositories.Invites;
using ProfileService.Infrastructure.Repositories.Devices;
using ProfileService.Infrastructure.Repositories.NotificationSettings;
using ProfileService.Infrastructure.Repositories.NotificationTypes;
using ProfileService.Infrastructure.Repositories.UserPreferences;
using ProfileService.Infrastructure.Repositories.PlatformAdmins;
using ProfileService.Infrastructure.Repositories.NavigationItems;
using ProfileService.Infrastructure.Services.Departments;
using ProfileService.Infrastructure.Services.Devices;
using ProfileService.Infrastructure.Services.ErrorCodeResolver;
using ProfileService.Infrastructure.Services.Invites;
using ProfileService.Infrastructure.Services.NotificationSettings;
using ProfileService.Infrastructure.Services.Navigation;
using ProfileService.Infrastructure.Services.Organizations;
using ProfileService.Infrastructure.Services.Outbox;
using ProfileService.Infrastructure.Services.PlatformAdmins;
using ProfileService.Infrastructure.Services.Preferences;
using ProfileService.Infrastructure.Services.Roles;
using ProfileService.Infrastructure.Services.ServiceClients;
using ProfileService.Infrastructure.Services.TeamMembers;
using StackExchange.Redis;

namespace ProfileService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, AppSettings appSettings)
    {
        // Database
        services.AddDbContext<ProfileDbContext>(options =>
            options.UseNpgsql(appSettings.DatabaseConnectionString));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(appSettings.RedisConnectionString));

        // Configuration
        services.AddSingleton(appSettings);

        // Repositories
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IDepartmentMemberRepository, DepartmentMemberRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IInviteRepository, InviteRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<INotificationSettingRepository, NotificationSettingRepository>();
        services.AddScoped<INotificationTypeRepository, NotificationTypeRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<IPlatformAdminRepository, PlatformAdminRepository>();
        services.AddScoped<INavigationItemRepository, NavigationItemRepository>();

        // Domain services
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ITeamMemberService, TeamMemberService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IInviteService, InviteService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<INotificationSettingService, NotificationSettingService>();
        services.AddScoped<IPreferenceService, PreferenceService>();
        services.AddScoped<IPreferenceResolver, PreferenceResolver>();
        services.AddScoped<IPlatformAdminService, PlatformAdminService>();
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IErrorCodeResolverService, ErrorCodeResolverService>();

        // Infrastructure service clients
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
