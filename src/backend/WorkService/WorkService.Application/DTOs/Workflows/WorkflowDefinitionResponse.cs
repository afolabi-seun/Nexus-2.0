namespace WorkService.Application.DTOs.Workflows;

public class WorkflowDefinitionResponse
{
    public Dictionary<string, List<string>> StoryTransitions { get; set; } = new();
    public Dictionary<string, List<string>> TaskTransitions { get; set; } = new();
}
