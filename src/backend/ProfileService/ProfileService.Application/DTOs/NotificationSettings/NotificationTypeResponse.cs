namespace ProfileService.Application.DTOs.NotificationSettings;

public class NotificationTypeResponse
{
    public Guid NotificationTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
