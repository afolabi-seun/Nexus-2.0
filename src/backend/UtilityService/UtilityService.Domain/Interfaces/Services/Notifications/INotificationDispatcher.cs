namespace UtilityService.Domain.Interfaces.Services.Notifications;

public interface INotificationDispatcher
{
    Task<bool> SendEmailAsync(string recipient, string subject, string htmlBody, CancellationToken ct = default);
    Task<bool> SendPushAsync(string deviceToken, string title, string body, CancellationToken ct = default);
    Task<bool> SendInAppAsync(Guid userId, string title, string body, CancellationToken ct = default);
}
