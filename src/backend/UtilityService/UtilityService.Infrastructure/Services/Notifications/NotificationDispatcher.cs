using Microsoft.Extensions.Logging;
using UtilityService.Domain.Interfaces.Services;

namespace UtilityService.Infrastructure.Services.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(ILogger<NotificationDispatcher> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendEmailAsync(string recipient, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("Email dispatched to {Recipient} with subject '{Subject}'", recipient, subject);
        return Task.FromResult(true);
    }

    public Task<bool> SendPushAsync(string deviceToken, string title, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Push notification sent to device {DeviceToken} with title '{Title}'", deviceToken, title);
        return Task.FromResult(true);
    }

    public Task<bool> SendInAppAsync(Guid userId, string title, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("In-app notification stored for user {UserId} with title '{Title}'", userId, title);
        return Task.FromResult(true);
    }
}
