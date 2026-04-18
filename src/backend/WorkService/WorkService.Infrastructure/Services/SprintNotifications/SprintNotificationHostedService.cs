using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Redis;
using WorkService.Infrastructure.Services.ServiceClients;

namespace WorkService.Infrastructure.Services.SprintNotifications;

public class SprintNotificationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SprintNotificationHostedService> _logger;

    public SprintNotificationHostedService(
        IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis,
        ILogger<SprintNotificationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for services to be ready
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckSprintsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sprint notification check failed.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }

    private async Task CheckSprintsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkDbContext>();
        var utilityClient = scope.ServiceProvider.GetRequiredService<IUtilityServiceClient>();
        var redis = _redis.GetDatabase();
        var now = DateTime.UtcNow;

        var activeSprints = await db.Sprints
            .Where(s => s.Status == "Active")
            .ToListAsync(ct);

        foreach (var sprint in activeSprints)
        {
            var dedupeKey = RedisKeys.SprintNotif(sprint.SprintId, now.ToString("yyyy-MM-dd"));

            // Sprint due soon (within 2 days)
            if (sprint.EndDate.Date <= now.AddDays(2).Date && sprint.EndDate.Date > now.Date)
            {
                var key = $"{dedupeKey}:due_soon";
                if (!await redis.KeyExistsAsync(key))
                {
                    await redis.StringSetAsync(key, "1", TimeSpan.FromHours(24));
                    await DispatchSprintNotificationAsync(utilityClient, db, sprint.OrganizationId,
                        "SprintDueSoon", $"Sprint \"{sprint.SprintName}\" ends on {sprint.EndDate:MMM dd}", sprint.SprintId, ct);
                }
            }

            // Sprint overdue (past end date)
            if (sprint.EndDate.Date < now.Date)
            {
                var key = $"{dedupeKey}:overdue";
                if (!await redis.KeyExistsAsync(key))
                {
                    await redis.StringSetAsync(key, "1", TimeSpan.FromHours(24));
                    await DispatchSprintNotificationAsync(utilityClient, db, sprint.OrganizationId,
                        "SprintOverdue", $"Sprint \"{sprint.SprintName}\" is overdue (ended {sprint.EndDate:MMM dd})", sprint.SprintId, ct);
                }
            }

            // Sprint at risk (>50% of time elapsed, <30% stories completed)
            var totalDays = (sprint.EndDate - sprint.StartDate).TotalDays;
            var elapsedDays = (now - sprint.StartDate).TotalDays;
            if (totalDays > 0 && elapsedDays / totalDays > 0.5)
            {
                var storyCount = await db.SprintStories.CountAsync(ss => ss.SprintId == sprint.SprintId, ct);
                var completedCount = storyCount > 0
                    ? await db.SprintStories
                        .Where(ss => ss.SprintId == sprint.SprintId)
                        .Join(db.Stories, ss => ss.StoryId, s => s.StoryId, (ss, s) => s)
                        .CountAsync(s => s.Status == "Done" || s.Status == "Closed", ct)
                    : 0;

                var completionRate = storyCount > 0 ? (double)completedCount / storyCount : 1.0;
                if (completionRate < 0.3)
                {
                    var key = $"{dedupeKey}:at_risk";
                    if (!await redis.KeyExistsAsync(key))
                    {
                        await redis.StringSetAsync(key, "1", TimeSpan.FromHours(24));
                        await DispatchSprintNotificationAsync(utilityClient, db, sprint.OrganizationId,
                            "SprintAtRisk", $"Sprint \"{sprint.SprintName}\" is at risk ({completedCount}/{storyCount} stories completed)", sprint.SprintId, ct);
                    }
                }
            }
        }

        _logger.LogInformation("Sprint notification check completed. Checked {Count} active sprints.", activeSprints.Count);
    }

    private async Task DispatchSprintNotificationAsync(
        IUtilityServiceClient utilityClient, WorkDbContext db,
        Guid organizationId, string notificationType, string subject,
        Guid sprintId, CancellationToken ct)
    {
        // Notify all team members in the sprint's project stories
        var memberIds = await db.SprintStories
            .Where(ss => ss.SprintId == sprintId)
            .Join(db.Stories, ss => ss.StoryId, s => s.StoryId, (ss, s) => s.AssigneeId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToListAsync(ct);

        foreach (var memberId in memberIds)
        {
            try
            {
                await utilityClient.DispatchNotificationAsync(
                    organizationId, memberId, memberId.ToString(),
                    notificationType, subject, "InApp,Email",
                    new Dictionary<string, string>
                    {
                        ["sprintId"] = sprintId.ToString(),
                        ["subject"] = subject
                    }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispatch {NotificationType} to member {MemberId}", notificationType, memberId);
            }
        }

        _logger.LogInformation("{NotificationType} dispatched for sprint {SprintId} to {MemberCount} members",
            notificationType, sprintId, memberIds.Count);
    }
}
