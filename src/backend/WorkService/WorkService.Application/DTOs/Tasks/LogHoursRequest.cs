namespace WorkService.Application.DTOs.Tasks;

public class LogHoursRequest
{
    public decimal Hours { get; set; }
    public string? Description { get; set; }
}
