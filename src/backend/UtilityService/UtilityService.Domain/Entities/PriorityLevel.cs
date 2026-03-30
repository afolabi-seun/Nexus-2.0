namespace UtilityService.Domain.Entities;

public class PriorityLevel
{
    public Guid PriorityLevelId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Color { get; set; } = string.Empty;
    public string FlgStatus { get; set; } = "A";
}
