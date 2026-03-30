namespace WorkService.Application.DTOs.Search;

public class SearchRequest
{
    public string? Query { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? SprintId { get; set; }
    public List<string>? Labels { get; set; }
    public string? EntityType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
