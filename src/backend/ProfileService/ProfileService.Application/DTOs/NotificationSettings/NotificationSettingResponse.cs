namespace ProfileService.Application.DTOs.NotificationSettings;

public class NotificationSettingResponse
{
    public Guid NotificationTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public bool IsEmail { get; set; }
    public bool IsPush { get; set; }
    public bool IsInApp { get; set; }
}
