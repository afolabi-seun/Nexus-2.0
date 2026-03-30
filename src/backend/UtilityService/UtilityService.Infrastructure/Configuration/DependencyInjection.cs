using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using UtilityService.Domain.Interfaces.Repositories;
using UtilityService.Domain.Interfaces.Services;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Repositories.AuditLogs;
using UtilityService.Infrastructure.Repositories.ArchivedAuditLogs;
using UtilityService.Infrastructure.Repositories.ErrorLogs;
using UtilityService.Infrastructure.Repositories.ErrorCodeEntries;
using UtilityService.Infrastructure.Repositories.NotificationLogs;
using UtilityService.Infrastructure.Repositories.DepartmentTypes;
using UtilityService.Infrastructure.Repositories.PriorityLevels;
using UtilityService.Infrastructure.Repositories.TaskTypeRefs;
using UtilityService.Infrastructure.Repositories.WorkflowStates;
using UtilityService.Infrastructure.Services.AuditLogs;
using UtilityService.Infrastructure.Services.BackgroundServices;
using UtilityService.Infrastructure.Services.ErrorCodeResolver;
using UtilityService.Infrastructure.Services.ErrorCodes;
using UtilityService.Infrastructure.Services.ErrorLogs;
using UtilityService.Infrastructure.Services.Notifications;
using UtilityService.Infrastructure.Services.Outbox;
using UtilityService.Infrastructure.Services.PiiRedaction;
using UtilityService.Infrastructure.Services.ReferenceData;

namespace UtilityService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, AppSettings appSettings)
    {
        // Configuration
        services.AddSingleton(appSettings);

        // DbContext
        services.AddDbContext<UtilityDbContext>(options =>
            options.UseNpgsql(appSettings.DatabaseConnectionString));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(appSettings.RedisConnectionString));

        // Repositories
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IArchivedAuditLogRepository, ArchivedAuditLogRepository>();
        services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
        services.AddScoped<IErrorCodeEntryRepository, ErrorCodeEntryRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IDepartmentTypeRepository, DepartmentTypeRepository>();
        services.AddScoped<IPriorityLevelRepository, PriorityLevelRepository>();
        services.AddScoped<ITaskTypeRefRepository, TaskTypeRefRepository>();
        services.AddScoped<IWorkflowStateRepository, WorkflowStateRepository>();

        // Services
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IErrorLogService, ErrorLogService>();
        services.AddScoped<IErrorCodeService, ErrorCodeService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<IReferenceDataService, ReferenceDataService>();
        services.AddScoped<IPiiRedactionService, PiiRedactionService>();
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();
        services.AddScoped<IOutboxMessageRouter, OutboxMessageRouter>();
        services.AddScoped<IErrorCodeResolverService, ErrorCodeResolverService>();

        // Background hosted services
        services.AddHostedService<OutboxProcessorHostedService>();
        services.AddHostedService<RetentionArchivalHostedService>();
        services.AddHostedService<NotificationRetryHostedService>();
        services.AddHostedService<DueDateNotificationHostedService>();

        // CorrelationId delegating handler for outgoing HTTP calls
        services.AddTransient<CorrelationIdDelegatingHandler>();

        return services;
    }
}
