namespace UtilityService.Application.DTOs.ReferenceData;

public class TaskTypeRefResponse
{
    public Guid TaskTypeRefId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string DefaultDepartmentCode { get; set; } = string.Empty;
}
