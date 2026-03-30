namespace UtilityService.Domain.Entities;

public class TaskTypeRef
{
    public Guid TaskTypeRefId { get; set; } = Guid.NewGuid();
    public string TypeName { get; set; } = string.Empty;
    public string DefaultDepartmentCode { get; set; } = string.Empty;
    public string FlgStatus { get; set; } = "A";
}
