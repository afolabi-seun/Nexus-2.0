namespace SecurityService.Application.DTOs.Otp;

public class OtpVerifyRequest
{
    public string Identity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
