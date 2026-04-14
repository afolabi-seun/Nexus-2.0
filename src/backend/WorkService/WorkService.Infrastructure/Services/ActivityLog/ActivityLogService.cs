using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Activity;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.ActivityLogs;
using WorkService.Domain.Interfaces.Services.ActivityLog;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.ActivityLog;

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _activityLogRepo;
    private readonly WorkDbContext _dbContext;

    public ActivityLogService(IActivityLogRepository activityLogRepo, WorkDbContext dbContext)
    {
        _activityLogRepo = activityLogRepo;
        _dbContext = dbContext;
    }

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
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<object> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        var logs = await _activityLogRepo.ListByEntityAsync(entityType, entityId, ct);
        return logs.Select(MapToResponse).ToList();
    }

    public async Task<object> GetOrganizationFeedAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _activityLogRepo.ListByOrganizationAsync(organizationId, page, pageSize, ct);
        return new PaginatedResponse<ActivityLogResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static ActivityLogResponse MapToResponse(Domain.Entities.ActivityLog l) => new()
    {
        ActivityLogId = l.ActivityLogId, EntityType = l.EntityType, EntityId = l.EntityId,
        StoryKey = l.StoryKey, Action = l.Action, ActorId = l.ActorId, ActorName = l.ActorName,
        OldValue = l.OldValue, NewValue = l.NewValue, Description = l.Description,
        DateCreated = l.DateCreated
    };
}
