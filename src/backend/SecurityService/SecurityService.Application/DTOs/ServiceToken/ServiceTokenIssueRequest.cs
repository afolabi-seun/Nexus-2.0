namespace SecurityService.Application.DTOs.ServiceToken;

public class ServiceTokenIssueRequest
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
}
