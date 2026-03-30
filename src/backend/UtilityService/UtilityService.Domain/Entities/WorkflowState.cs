namespace UtilityService.Domain.Entities;

public class WorkflowState
{
    public Guid WorkflowStateId { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string FlgStatus { get; set; } = "A";
}
