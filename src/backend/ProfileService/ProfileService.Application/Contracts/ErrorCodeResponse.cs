namespace ProfileService.Application.Contracts;

public class ErrorCodeResponse
{
    public string ResponseCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
