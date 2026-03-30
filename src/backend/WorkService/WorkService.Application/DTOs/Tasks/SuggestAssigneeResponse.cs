namespace WorkService.Application.DTOs.Tasks;

public class SuggestAssigneeResponse
{
    public Guid? SuggestedAssigneeId { get; set; }
    public string? SuggestedAssigneeName { get; set; }
    public string? DepartmentName { get; set; }
    public int ActiveTaskCount { get; set; }
    public int MaxConcurrentTasks { get; set; }
}
