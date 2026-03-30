namespace ProfileService.Application.DTOs.Navigation;

public class NavigationItemResponse
{
    public Guid NavigationItemId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? ParentId { get; set; }
    public int MinPermissionLevel { get; set; }
    public bool IsEnabled { get; set; }
    public List<NavigationItemResponse> Children { get; set; } = new();
}

public class CreateNavigationItemRequest
{
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? ParentId { get; set; }
    public int MinPermissionLevel { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateNavigationItemRequest
{
    public string? Label { get; set; }
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public int? SortOrder { get; set; }
    public int? MinPermissionLevel { get; set; }
    public bool? IsEnabled { get; set; }
}
