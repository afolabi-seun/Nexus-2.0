namespace UtilityService.Domain.Entities;

public class ErrorCodeEntry
{
    public Guid ErrorCodeEntryId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public int Value { get; set; }
    public int HttpStatusCode { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}
