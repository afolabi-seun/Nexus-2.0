namespace WorkService.Application.DTOs.Sprints;

public class BurndownDataPoint
{
    public DateTime Date { get; set; }
    public int RemainingPoints { get; set; }
    public decimal IdealRemainingPoints { get; set; }
}
