namespace UtilityService.Application.DTOs.ReferenceData;

public class DepartmentTypeResponse
{
    public Guid DepartmentTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
}
