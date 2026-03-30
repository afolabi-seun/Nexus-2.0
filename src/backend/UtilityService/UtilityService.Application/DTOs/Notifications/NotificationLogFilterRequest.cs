namespace UtilityService.Application.DTOs.Notifications;

public class NotificationLogFilterRequest
{
    public string? NotificationType { get; set; }
    public string? Channel { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
