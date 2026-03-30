namespace WorkService.Application.DTOs.Workflows;

public class WorkflowOverrideRequest
{
    public Dictionary<string, List<string>>? StoryTransitions { get; set; }
    public Dictionary<string, List<string>>? TaskTransitions { get; set; }
    public List<string>? CustomStatuses { get; set; }
}
