namespace WorkService.Application.DTOs.Sprints;

public class VelocityResponse
{
    public string SprintName { get; set; } = string.Empty;
    public int Velocity { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
