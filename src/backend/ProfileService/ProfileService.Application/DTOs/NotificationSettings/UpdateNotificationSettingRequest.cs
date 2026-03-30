namespace ProfileService.Application.DTOs.NotificationSettings;

public class UpdateNotificationSettingRequest
{
    public bool IsEmail { get; set; }
    public bool IsPush { get; set; }
    public bool IsInApp { get; set; }
}
