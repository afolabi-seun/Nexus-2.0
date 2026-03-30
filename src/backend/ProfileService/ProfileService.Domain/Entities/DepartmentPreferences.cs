namespace ProfileService.Domain.Entities;

public class DepartmentPreferences
{
    public string[] DefaultTaskTypes { get; set; } = [];
    public object? CustomWorkflowOverrides { get; set; }
    public Dictionary<string, int>? WipLimitPerStatus { get; set; }
    public Guid? DefaultAssigneeId { get; set; }
    public object? NotificationChannelOverrides { get; set; }
    public int MaxConcurrentTasksDefault { get; set; } = 5;
}
