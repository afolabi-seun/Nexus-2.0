namespace UtilityService.Domain.Entities;

public class ArchivedAuditLog
{
    public Guid ArchivedAuditLogId { get; set; } = Guid.NewGuid();
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
    public DateTime DateCreated { get; set; }
    public DateTime ArchivedDate { get; set; } = DateTime.UtcNow;
}
