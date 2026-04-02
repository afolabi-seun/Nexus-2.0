using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.Notifications;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Helpers;
using UtilityService.Domain.Interfaces.Repositories.NotificationLogs;
using UtilityService.Domain.Interfaces.Services.Notifications;
using UtilityService.Infrastructure.Configuration;
using UtilityService.Infrastructure.Data;

namespace UtilityService.Infrastructure.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly INotificationLogRepository _repo;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;
    private readonly ILogger<NotificationService> _logger;
    private readonly UtilityDbContext _dbContext;

    public NotificationService(
        INotificationLogRepository repo, INotificationDispatcher dispatcher,
        ITemplateRenderer templateRenderer, IConnectionMultiplexer redis,
        AppSettings appSettings, ILogger<NotificationService> logger,
        UtilityDbContext dbContext)
    {
        _repo = repo;
        _dispatcher = dispatcher;
        _templateRenderer = templateRenderer;
        _redis = redis;
        _appSettings = appSettings;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task DispatchAsync(object request, CancellationToken ct = default)
    {
        var req = (DispatchNotificationRequest)request;
        var channels = req.Channels.Split(',').Select(c => c.Trim()).ToList();

        foreach (var channel in channels)
        {
            var renderedContent = _templateRenderer.Render(req.NotificationType, channel, req.TemplateVariables);
            var log = new NotificationLog
            {
                OrganizationId = req.OrganizationId,
                UserId = req.UserId,
                NotificationType = req.NotificationType,
                Channel = channel,
                Recipient = req.Recipient,
                Subject = req.Subject,
                Status = NotificationStatuses.Pending
            };

            await _repo.AddAsync(log, ct);
            await _dbContext.SaveChangesAsync(ct);

            try
            {
                var success = channel switch
                {
                    NotificationChannels.Email => await _dispatcher.SendEmailAsync(req.Recipient, req.Subject ?? req.NotificationType, renderedContent, ct),
                    NotificationChannels.Push => await _dispatcher.SendPushAsync(req.Recipient, req.Subject ?? req.NotificationType, renderedContent, ct),
                    NotificationChannels.InApp => await _dispatcher.SendInAppAsync(req.UserId, req.Subject ?? req.NotificationType, renderedContent, ct),
                    _ => false
                };

                log.Status = success ? NotificationStatuses.Sent : NotificationStatuses.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notification dispatch failed. NotificationType={NotificationType}, Channel={Channel}, Recipient={Recipient}",
                    req.NotificationType, channel, req.Recipient);
                log.Status = NotificationStatuses.Failed;
            }

            await _repo.UpdateAsync(log, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<object> GetUserHistoryAsync(Guid userId, Guid organizationId, object filter, int page, int pageSize, CancellationToken ct = default)
    {
        var f = (NotificationLogFilterRequest)filter;
        var (items, totalCount) = await _repo.QueryByUserAsync(
            userId, organizationId, f.NotificationType, f.Channel, f.Status, f.DateFrom, f.DateTo, page, pageSize, ct);

        return new PaginatedResponse<NotificationLogResponse>
        {
            TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data = items.Select(MapToResponse)
        };
    }

    public async Task RetryFailedAsync(CancellationToken ct = default)
    {
        var failed = await _repo.GetFailedForRetryAsync(_appSettings.NotificationRetryMax, ct);
        foreach (var log in failed)
        {
            log.RetryCount++;
            log.LastRetryDate = DateTime.UtcNow;

            try
            {
                var success = log.Channel switch
                {
                    NotificationChannels.Email => await _dispatcher.SendEmailAsync(log.Recipient, log.Subject ?? log.NotificationType, "", ct),
                    NotificationChannels.Push => await _dispatcher.SendPushAsync(log.Recipient, log.Subject ?? log.NotificationType, "", ct),
                    NotificationChannels.InApp => await _dispatcher.SendInAppAsync(log.UserId, log.Subject ?? log.NotificationType, "", ct),
                    _ => false
                };

                log.Status = success ? NotificationStatuses.Sent : NotificationStatuses.Failed;
            }
            catch
            {
                log.Status = log.RetryCount >= _appSettings.NotificationRetryMax
                    ? NotificationStatuses.PermanentlyFailed
                    : NotificationStatuses.Failed;
            }

            if (log.RetryCount >= _appSettings.NotificationRetryMax && log.Status == NotificationStatuses.Failed)
                log.Status = NotificationStatuses.PermanentlyFailed;

            await _repo.UpdateAsync(log, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    private static NotificationLogResponse MapToResponse(NotificationLog e) => new()
    {
        NotificationLogId = e.NotificationLogId, OrganizationId = e.OrganizationId,
        UserId = e.UserId, NotificationType = e.NotificationType, Channel = e.Channel,
        Recipient = e.Recipient, Subject = e.Subject, Status = e.Status,
        RetryCount = e.RetryCount, LastRetryDate = e.LastRetryDate, DateCreated = e.DateCreated
    };
}
