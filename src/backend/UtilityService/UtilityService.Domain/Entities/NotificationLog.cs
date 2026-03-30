using UtilityService.Domain.Common;

namespace UtilityService.Domain.Entities;

public class NotificationLog : IOrganizationEntity
{
    public Guid NotificationLogId { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; } = 0;
    public DateTime? LastRetryDate { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
