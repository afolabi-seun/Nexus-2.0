namespace UtilityService.Domain.Entities;

public class DepartmentType
{
    public Guid DepartmentTypeId { get; set; } = Guid.NewGuid();
    public string TypeName { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string FlgStatus { get; set; } = "A";
}
