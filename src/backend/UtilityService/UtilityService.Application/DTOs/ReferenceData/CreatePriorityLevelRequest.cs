namespace UtilityService.Application.DTOs.ReferenceData;

public class CreatePriorityLevelRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Color { get; set; } = string.Empty;
}
