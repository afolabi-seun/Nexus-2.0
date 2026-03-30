namespace UtilityService.Application.DTOs.Notifications;

public class NotificationLogResponse
{
    public Guid NotificationLogId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime? LastRetryDate { get; set; }
    public DateTime DateCreated { get; set; }
}
