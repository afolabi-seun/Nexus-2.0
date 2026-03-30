namespace SecurityService.Application.DTOs.Auth;

public class CredentialGenerateRequest
{
    public Guid MemberId { get; set; }
    public string Email { get; set; } = string.Empty;
}
