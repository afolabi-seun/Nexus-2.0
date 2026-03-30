namespace WorkService.Application.DTOs.SavedFilters;

public class SavedFilterResponse
{
    public Guid SavedFilterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Filters { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
}
