namespace ProfileService.Application.DTOs.Departments;

public class DepartmentPreferencesRequest
{
    public string[]? DefaultTaskTypes { get; set; }
    public object? CustomWorkflowOverrides { get; set; }
    public Dictionary<string, int>? WipLimitPerStatus { get; set; }
    public Guid? DefaultAssigneeId { get; set; }
    public object? NotificationChannelOverrides { get; set; }
    public int? MaxConcurrentTasksDefault { get; set; }
}
