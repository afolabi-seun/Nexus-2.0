namespace SecurityService.Application.Contracts;

public class PlatformAdminResponse
{
    public Guid PlatformAdminId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FlgStatus { get; set; } = string.Empty;
    public bool IsFirstTimeUser { get; set; }
    public string Email { get; set; } = string.Empty;
}
