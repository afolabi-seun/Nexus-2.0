namespace UtilityService.Application.DTOs.ErrorCodes;

public class ErrorCodeResponse
{
    public Guid ErrorCodeEntryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Value { get; set; }
    public int HttpStatusCode { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public DateTime DateUpdated { get; set; }
}
