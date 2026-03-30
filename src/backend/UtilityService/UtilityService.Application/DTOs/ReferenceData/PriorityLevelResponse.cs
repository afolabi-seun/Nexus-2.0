namespace UtilityService.Application.DTOs.ReferenceData;

public class PriorityLevelResponse
{
    public Guid PriorityLevelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Color { get; set; } = string.Empty;
}
