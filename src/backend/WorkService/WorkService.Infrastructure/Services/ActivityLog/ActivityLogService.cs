using WorkService.Application.DTOs.Activity;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Domain.Interfaces.Services;

namespace WorkService.Infrastructure.Services.ActivityLog;

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _activityLogRepo;

    public ActivityLogService(IActivityLogRepository activityLogRepo) => _activityLogRepo = activityLogRepo;

    public async System.Threading.Tasks.Task LogAsync(
        Guid organizationId, string entityType, Guid entityId, string storyKey,
        string action, Guid actorId, string actorName, string? oldValue, string? newValue,
        string description, CancellationToken ct = default)
    {
        await _activityLogRepo.AddAsync(new Domain.Entities.ActivityLog
        {
            OrganizationId = organizationId, EntityType = entityType, EntityId = entityId,
            StoryKey = storyKey, Action = action, ActorId = actorId, ActorName = actorName,
            OldValue = oldValue, NewValue = newValue, Description = description
        }, ct);
    }

    public async Task<object> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        var logs = await _activityLogRepo.ListByEntityAsync(entityType, entityId, ct);
        return logs.Select(l => new ActivityLogResponse
        {
            ActivityLogId = l.ActivityLogId, EntityType = l.EntityType, EntityId = l.EntityId,
            StoryKey = l.StoryKey, Action = l.Action, ActorId = l.ActorId, ActorName = l.ActorName,
            OldValue = l.OldValue, NewValue = l.NewValue, Description = l.Description,
            DateCreated = l.DateCreated
        }).ToList();
    }
}
