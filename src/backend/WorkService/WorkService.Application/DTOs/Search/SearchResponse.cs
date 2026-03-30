namespace WorkService.Application.DTOs.Search;

public class SearchResponse
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<SearchResultItem> Items { get; set; } = new();
}

public class SearchResultItem
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? StoryKey { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
    public string? DepartmentName { get; set; }
    public decimal Relevance { get; set; }
}
