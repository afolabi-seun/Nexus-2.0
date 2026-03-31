namespace WorkService.Application.DTOs.TimeEntries;

public class MemberCostDetail
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal Cost { get; set; }
}
