using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class SavedFilter : IOrganizationEntity
{
    public Guid SavedFilterId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid TeamMemberId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Filters { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
