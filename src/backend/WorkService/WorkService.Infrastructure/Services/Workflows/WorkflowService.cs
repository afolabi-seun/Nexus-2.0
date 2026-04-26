using System.Text.Json;
using WorkService.Application.DTOs.Workflows;
using WorkService.Domain.Helpers;
using WorkService.Domain.Interfaces.Services.Workflows;
using WorkService.Domain.Results;
using StackExchange.Redis;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.Workflows;

public class WorkflowService : IWorkflowService
{
    private readonly IConnectionMultiplexer _redis;

    public WorkflowService(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<ServiceResult<object>> GetWorkflowsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // Check for org-level overrides
        var orgOverride = await db.StringGetAsync(RedisKeys.WorkflowOrg(organizationId));
        if (orgOverride.HasValue)
        {
            var overrideResult = JsonSerializer.Deserialize<WorkflowDefinitionResponse>(orgOverride!);
            if (overrideResult != null) return ServiceResult<object>.Ok(overrideResult);
        }

        // Return defaults from WorkflowStateMachine
        var storyTransitions = WorkflowStateMachine.GetStoryTransitions()
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
        var taskTransitions = WorkflowStateMachine.GetTaskTransitions()
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToList());

        return ServiceResult<object>.Ok(new WorkflowDefinitionResponse
        {
            StoryTransitions = storyTransitions,
            TaskTransitions = taskTransitions
        });
    }

    public async Task<ServiceResult<object>> SaveOrganizationOverrideAsync(
        Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (WorkflowOverrideRequest)request;
        var db = _redis.GetDatabase();
        var response = new WorkflowDefinitionResponse
        {
            StoryTransitions = req.StoryTransitions ?? new(),
            TaskTransitions = req.TaskTransitions ?? new()
        };
        var json = JsonSerializer.Serialize(response);
        await db.StringSetAsync(RedisKeys.WorkflowOrg(organizationId), json);
        return ServiceResult<object>.NoContent("Organization workflow override saved.");
    }

    public async Task<ServiceResult<object>> SaveDepartmentOverrideAsync(
        Guid organizationId, Guid departmentId, object request, CancellationToken ct = default)
    {
        var req = (WorkflowOverrideRequest)request;
        var db = _redis.GetDatabase();
        var response = new WorkflowDefinitionResponse
        {
            StoryTransitions = req.StoryTransitions ?? new(),
            TaskTransitions = req.TaskTransitions ?? new()
        };
        var json = JsonSerializer.Serialize(response);
        await db.StringSetAsync(RedisKeys.WorkflowDept(organizationId, departmentId), json);
        return ServiceResult<object>.NoContent("Department workflow override saved.");
    }
}
