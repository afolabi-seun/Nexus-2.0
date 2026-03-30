namespace WorkService.Application.DTOs.SavedFilters;

public class CreateSavedFilterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Filters { get; set; } = string.Empty;
}
