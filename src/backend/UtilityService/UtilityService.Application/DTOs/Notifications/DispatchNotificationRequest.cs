namespace UtilityService.Application.DTOs.Notifications;

public class DispatchNotificationRequest
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channels { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public Dictionary<string, string> TemplateVariables { get; set; } = new();
}
