namespace UtilityService.Application.DTOs.ReferenceData;

public class WorkflowStateResponse
{
    public Guid WorkflowStateId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
