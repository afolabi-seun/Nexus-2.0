namespace UtilityService.Application.DTOs.AuditLogs;

public class AuditLogFilterRequest
{
    public string? ServiceName { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? UserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
