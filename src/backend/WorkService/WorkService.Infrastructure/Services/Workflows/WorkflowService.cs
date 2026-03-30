using System.Text.Json;
using WorkService.Application.DTOs.Workflows;
using WorkService.Domain.Helpers;
using WorkService.Domain.Interfaces.Services;
using StackExchange.Redis;

namespace WorkService.Infrastructure.Services.Workflows;

public class WorkflowService : IWorkflowService
{
    private readonly IConnectionMultiplexer _redis;

    public WorkflowService(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<object> GetWorkflowsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // Check for org-level overrides
        var orgOverride = await db.StringGetAsync($"workflow_override:org:{organizationId}");
        if (orgOverride.HasValue)
        {
            var overrideResult = JsonSerializer.Deserialize<WorkflowDefinitionResponse>(orgOverride!);
            if (overrideResult != null) return overrideResult;
        }

        // Return defaults from WorkflowStateMachine
        var storyTransitions = WorkflowStateMachine.GetStoryTransitions()
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
        var taskTransitions = WorkflowStateMachine.GetTaskTransitions()
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToList());

        return new WorkflowDefinitionResponse
        {
            StoryTransitions = storyTransitions,
            TaskTransitions = taskTransitions
        };
    }

    public async System.Threading.Tasks.Task SaveOrganizationOverrideAsync(
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
        await db.StringSetAsync($"workflow_override:org:{organizationId}", json);
    }

    public async System.Threading.Tasks.Task SaveDepartmentOverrideAsync(
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
        await db.StringSetAsync($"workflow_override:dept:{organizationId}:{departmentId}", json);
    }
}
