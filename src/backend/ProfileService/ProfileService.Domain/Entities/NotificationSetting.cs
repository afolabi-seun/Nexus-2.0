using ProfileService.Domain.Common;

namespace ProfileService.Domain.Entities;

public class NotificationSetting : IOrganizationEntity
{
    public Guid NotificationSettingId { get; set; } = Guid.NewGuid();
    public Guid NotificationTypeId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TeamMemberId { get; set; }
    public bool IsEmail { get; set; }
    public bool IsPush { get; set; }
    public bool IsInApp { get; set; }

    // Navigation
    public NotificationType NotificationType { get; set; } = null!;
    public TeamMember TeamMember { get; set; } = null!;
}
