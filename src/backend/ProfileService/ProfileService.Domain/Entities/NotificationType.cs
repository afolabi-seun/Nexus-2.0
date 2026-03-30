namespace ProfileService.Domain.Entities;

public class NotificationType
{
    public Guid NotificationTypeId { get; set; } = Guid.NewGuid();
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
