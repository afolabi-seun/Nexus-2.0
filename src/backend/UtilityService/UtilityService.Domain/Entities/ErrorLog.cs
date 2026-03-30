using UtilityService.Domain.Common;

namespace UtilityService.Domain.Entities;

public class ErrorLog : IOrganizationEntity
{
    public Guid ErrorLogId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
