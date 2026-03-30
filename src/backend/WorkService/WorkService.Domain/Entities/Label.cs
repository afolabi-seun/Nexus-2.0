using WorkService.Domain.Common;

namespace WorkService.Domain.Entities;

public class Label : IOrganizationEntity
{
    public Guid LabelId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
