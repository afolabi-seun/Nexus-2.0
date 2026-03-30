namespace ProfileService.Domain.Entities;

public class NavigationItem
{
    public Guid NavigationItemId { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Guid? ParentId { get; set; }
    public int MinPermissionLevel { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    // Navigation
    public NavigationItem? Parent { get; set; }
    public ICollection<NavigationItem> Children { get; set; } = new List<NavigationItem>();
}
