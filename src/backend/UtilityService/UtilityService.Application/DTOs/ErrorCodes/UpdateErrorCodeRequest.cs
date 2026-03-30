namespace UtilityService.Application.DTOs.ErrorCodes;

public class UpdateErrorCodeRequest
{
    public int? Value { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ResponseCode { get; set; }
    public string? Description { get; set; }
    public string? ServiceName { get; set; }
}
