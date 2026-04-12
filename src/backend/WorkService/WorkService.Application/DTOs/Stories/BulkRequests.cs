namespace WorkService.Application.DTOs.Stories;

public class BulkStatusRequest
{
    public List<Guid> StoryIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}

public class BulkAssignRequest
{
    public List<Guid> StoryIds { get; set; } = new();
    public Guid AssigneeId { get; set; }
}
