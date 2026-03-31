namespace WorkService.Application.DTOs.TimeEntries;

public class ResourceUtilizationResponse
{
    public List<MemberUtilizationDetail> Members { get; set; } = new();
}
