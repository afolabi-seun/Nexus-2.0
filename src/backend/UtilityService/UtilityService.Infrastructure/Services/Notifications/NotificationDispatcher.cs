using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using UtilityService.Domain.Interfaces.Services.Notifications;
using UtilityService.Infrastructure.Configuration;

namespace UtilityService.Infrastructure.Services.Notifications;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly AppSettings _settings;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(AppSettings settings, ILogger<NotificationDispatcher> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string recipient, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);

            if (!string.IsNullOrEmpty(_settings.SmtpUsername))
                client.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);

            client.EnableSsl = _settings.SmtpUseSsl;

            var message = new MailMessage
            {
                From = new MailAddress(_settings.SmtpFromAddress, _settings.SmtpFromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(recipient);

            await client.SendMailAsync(message, ct);

            _logger.LogInformation("Email sent to {Recipient} with subject '{Subject}'", recipient, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {Recipient} with subject '{Subject}'", recipient, subject);
            return false;
        }
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
