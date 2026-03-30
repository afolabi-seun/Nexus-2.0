namespace ProfileService.Application.DTOs.Departments;

public class CreateDepartmentRequest
{
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string? Description { get; set; }
}
