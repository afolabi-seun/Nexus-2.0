using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class StoryLink : IOrganizationEntity
{
    public Guid StoryLinkId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid SourceStoryId { get; set; }
    public Guid TargetStoryId { get; set; }
    public string LinkType { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
