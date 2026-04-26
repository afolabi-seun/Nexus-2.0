using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using WorkService.Domain.Interfaces.Repositories.ActivityLogs;
using WorkService.Domain.Interfaces.Repositories.Comments;
using WorkService.Domain.Interfaces.Repositories.Labels;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.SavedFilters;
using WorkService.Domain.Interfaces.Repositories.SprintStories;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.StoryLabels;
using WorkService.Domain.Interfaces.Repositories.StoryLinks;
using WorkService.Domain.Interfaces.Repositories.StorySequences;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.ActivityLog;
using WorkService.Domain.Interfaces.Services.Export;
using WorkService.Domain.Interfaces.Services.Boards;
using WorkService.Domain.Interfaces.Services.Comments;
using WorkService.Domain.Interfaces.Services.ErrorCodeResolver;
using WorkService.Domain.Interfaces.Services.Labels;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Domain.Interfaces.Services.Projects;
using WorkService.Domain.Interfaces.Services.Reports;
using WorkService.Domain.Interfaces.Services.Search;
using WorkService.Domain.Interfaces.Services.Sprints;
using WorkService.Domain.Interfaces.Services.Stories;
using WorkService.Domain.Interfaces.Services.Tasks;
using WorkService.Domain.Interfaces.Services.Workflows;
using WorkService.Domain.Interfaces.Repositories.CostRates;
using WorkService.Domain.Interfaces.Repositories.CostSnapshots;
using WorkService.Domain.Interfaces.Repositories.ProjectHealthSnapshots;
using WorkService.Domain.Interfaces.Repositories.ResourceAllocationSnapshots;
using WorkService.Domain.Interfaces.Repositories.RiskRegisters;
using WorkService.Domain.Interfaces.Repositories.StoryTemplates;
using WorkService.Domain.Interfaces.Repositories.TimeApprovals;
using WorkService.Domain.Interfaces.Repositories.TimeEntries;
using WorkService.Domain.Interfaces.Repositories.TimePolicies;
using WorkService.Domain.Interfaces.Repositories.VelocitySnapshots;
using WorkService.Domain.Interfaces.Services.Analytics;
using WorkService.Domain.Interfaces.Services.CostRates;
using WorkService.Domain.Interfaces.Services.CostSnapshots;
using WorkService.Domain.Interfaces.Services.RiskRegisters;
using WorkService.Domain.Interfaces.Services.StoryTemplates;
using WorkService.Domain.Interfaces.Services.SavedFilters;
using WorkService.Domain.Interfaces.Services.TimeEntries;
using WorkService.Domain.Interfaces.Services.TimePolicies;
using WorkService.Domain.Interfaces.Services.TimerSessions;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Projects;
using WorkService.Infrastructure.Repositories.Stories;
using WorkService.Infrastructure.Repositories.StorySequences;
using WorkService.Infrastructure.Repositories.Tasks;
using WorkService.Infrastructure.Repositories.Sprints;
using WorkService.Infrastructure.Repositories.SprintStories;
using WorkService.Infrastructure.Repositories.Comments;
using WorkService.Infrastructure.Repositories.Labels;
using WorkService.Infrastructure.Repositories.StoryLabels;
using WorkService.Infrastructure.Repositories.StoryLinks;
using WorkService.Infrastructure.Repositories.ActivityLogs;
using WorkService.Infrastructure.Repositories.SavedFilters;
using WorkService.Infrastructure.Repositories.CostRates;
using WorkService.Infrastructure.Repositories.CostSnapshots;
using WorkService.Infrastructure.Repositories.ProjectHealthSnapshots;
using WorkService.Infrastructure.Repositories.ResourceAllocationSnapshots;
using WorkService.Infrastructure.Repositories.RiskRegisters;
using WorkService.Infrastructure.Repositories.StoryTemplates;
using WorkService.Infrastructure.Repositories.TimeApprovals;
using WorkService.Infrastructure.Repositories.TimeEntries;
using WorkService.Infrastructure.Repositories.TimePolicies;
using WorkService.Infrastructure.Repositories.VelocitySnapshots;
using WorkService.Infrastructure.Services.Analytics;
using WorkService.Infrastructure.Services.Boards;
using WorkService.Infrastructure.Services.Comments;
using WorkService.Infrastructure.Services.ErrorCodeResolver;
using WorkService.Infrastructure.Services.Labels;
using WorkService.Infrastructure.Services.Outbox;
using WorkService.Infrastructure.Services.Projects;
using WorkService.Infrastructure.Services.Reports;
using WorkService.Infrastructure.Services.Search;
using WorkService.Infrastructure.Services.ServiceClients;
using WorkService.Infrastructure.Services.Sprints;
using WorkService.Infrastructure.Services.Stories;
using WorkService.Infrastructure.Services.Tasks;
using WorkService.Infrastructure.Services.Workflows;
using WorkService.Infrastructure.Services.CostRates;
using WorkService.Infrastructure.Services.CostSnapshots;
using WorkService.Infrastructure.Services.SprintNotifications;
using WorkService.Infrastructure.Services.RiskRegisters;
using WorkService.Infrastructure.Services.StoryTemplates;
using WorkService.Infrastructure.Services.SavedFilters;
using WorkService.Infrastructure.Services.TimeEntries;
using WorkService.Infrastructure.Services.TimePolicies;
using WorkService.Infrastructure.Services.TimerSessions;
using StackExchange.Redis;
using ActivityLogService = WorkService.Infrastructure.Services.ActivityLog.ActivityLogService;
using ExportService = WorkService.Infrastructure.Services.Export.ExportService;

namespace WorkService.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, AppSettings appSettings)
    {
        // Database
        services.AddDbContext<WorkDbContext>(options =>
            options.UseNpgsql(appSettings.DatabaseConnectionString));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(appSettings.RedisConnectionString));

        // Configuration
        services.AddSingleton(appSettings);

        // Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IStoryRepository, StoryRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ISprintStoryRepository, SprintStoryRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IStoryLabelRepository, StoryLabelRepository>();
        services.AddScoped<IStoryLinkRepository, StoryLinkRepository>();
        services.AddScoped<IStorySequenceRepository, StorySequenceRepository>();
        services.AddScoped<ISavedFilterRepository, SavedFilterRepository>();
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<ICostRateRepository, CostRateRepository>();
        services.AddScoped<ITimePolicyRepository, TimePolicyRepository>();
        services.AddScoped<ITimeApprovalRepository, TimeApprovalRepository>();
        services.AddScoped<ICostSnapshotRepository, CostSnapshotRepository>();

        // Analytics repositories
        services.AddScoped<IRiskRegisterRepository, RiskRegisterRepository>();
        services.AddScoped<IStoryTemplateRepository, StoryTemplateRepository>();
        services.AddScoped<IVelocitySnapshotRepository, VelocitySnapshotRepository>();
        services.AddScoped<IProjectHealthSnapshotRepository, ProjectHealthSnapshotRepository>();
        services.AddScoped<IResourceAllocationSnapshotRepository, ResourceAllocationSnapshotRepository>();

        // Domain services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IStoryIdGenerator, StoryIdGenerator>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IErrorCodeResolverService, ErrorCodeResolverService>();
        services.AddScoped<ITimeEntryService, TimeEntryService>();
        services.AddScoped<ICostRateService, CostRateService>();
        services.AddScoped<ICostRateResolver, CostRateResolver>();
        services.AddScoped<ITimePolicyService, TimePolicyService>();
        services.AddScoped<ITimerSessionService, TimerSessionService>();
        services.AddScoped<ICostSnapshotService, CostSnapshotHostedService>();

        // Analytics services
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IHealthScoreCalculator, HealthScoreCalculator>();
        services.AddScoped<IDependencyAnalyzer, DependencyAnalyzer>();
        services.AddScoped<IRiskRegisterService, RiskRegisterService>();
        services.AddScoped<IStoryTemplateService, StoryTemplateService>();
        services.AddScoped<IAnalyticsSnapshotService, AnalyticsSnapshotHostedService>();
        services.AddScoped<ISavedFilterService, SavedFilterService>();

        // Background services
        services.AddHostedService<CostSnapshotHostedService>();
        services.AddHostedService<AnalyticsSnapshotHostedService>();
        services.AddHostedService<SprintNotificationHostedService>();
        services.AddHostedService<Services.BackgroundServices.ErrorCodeValidationHostedService>();
        services.AddHostedService<Services.BackgroundServices.ErrorCodeCacheRefreshService>();

        // Infrastructure service clients
        services.AddScoped<IProfileServiceClient, ProfileServiceClient>();
        services.AddScoped<ISecurityServiceClient, SecurityServiceClient>();
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

        return services;
    }
}
