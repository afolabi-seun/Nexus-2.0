namespace WorkService.Application.DTOs.Stories;

public class CreateStoryLinkRequest
{
    public Guid TargetStoryId { get; set; }
    public string LinkType { get; set; } = string.Empty;
}
