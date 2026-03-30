namespace UtilityService.Application.DTOs.ErrorLogs;

public class CreateErrorLogRequest
{
    public Guid OrganizationId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}
