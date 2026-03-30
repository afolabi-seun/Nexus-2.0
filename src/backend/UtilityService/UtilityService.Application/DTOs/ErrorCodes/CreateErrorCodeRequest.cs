namespace UtilityService.Application.DTOs.ErrorCodes;

public class CreateErrorCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public int Value { get; set; }
    public int HttpStatusCode { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
}
