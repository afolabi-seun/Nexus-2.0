using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Domain.Interfaces.Services;
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
using StackExchange.Redis;
using ActivityLogService = WorkService.Infrastructure.Services.ActivityLog.ActivityLogService;

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

        // Domain services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IStoryIdGenerator, StoryIdGenerator>();
        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<IErrorCodeResolverService, ErrorCodeResolverService>();

        // Infrastructure service clients
        services.AddScoped<IProfileServiceClient, ProfileServiceClient>();
        services.AddScoped<ISecurityServiceClient, SecurityServiceClient>();

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
