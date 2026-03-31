namespace WorkService.Application.DTOs.TimeEntries;

public class DepartmentCostDetail
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal Cost { get; set; }
}
