namespace UtilityService.Application.DTOs.ErrorLogs;

public class ErrorLogFilterRequest
{
    public string? ServiceName { get; set; }
    public string? ErrorCode { get; set; }
    public string? Severity { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
