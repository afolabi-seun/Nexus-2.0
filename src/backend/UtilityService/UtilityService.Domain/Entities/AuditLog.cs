using UtilityService.Domain.Common;

namespace UtilityService.Domain.Entities;

public class AuditLog : IOrganizationEntity
{
    public Guid AuditLogId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
