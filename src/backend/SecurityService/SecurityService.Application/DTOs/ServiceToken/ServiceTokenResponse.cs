namespace SecurityService.Application.DTOs.ServiceToken;

public class ServiceTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
