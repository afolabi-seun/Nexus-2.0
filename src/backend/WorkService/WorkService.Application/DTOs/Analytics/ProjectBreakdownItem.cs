namespace WorkService.Application.DTOs.Analytics;

public class ProjectBreakdownItem
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public decimal Percentage { get; set; }
}
