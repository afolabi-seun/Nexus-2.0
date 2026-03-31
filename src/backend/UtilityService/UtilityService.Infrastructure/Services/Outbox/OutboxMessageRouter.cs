using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.AuditLogs;
using UtilityService.Application.DTOs.Notifications;
using UtilityService.Domain.Interfaces.Services.AuditLogs;
using UtilityService.Domain.Interfaces.Services.Notifications;
using UtilityService.Domain.Interfaces.Services.Outbox;

namespace UtilityService.Infrastructure.Services.Outbox;

public class OutboxMessageRouter : IOutboxMessageRouter
{
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<OutboxMessageRouter> _logger;

    public OutboxMessageRouter(
        IAuditLogService auditLogService, INotificationService notificationService,
        IConnectionMultiplexer redis, ILogger<OutboxMessageRouter> logger)
    {
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _redis = redis;
        _logger = logger;
    }

    public async Task RouteAsync(string rawMessage, string sourceQueue, CancellationToken ct = default)
    {
        OutboxMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<OutboxMessage>(rawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize outbox message from {Queue}", sourceQueue);
            await MoveToDlqAsync(sourceQueue, rawMessage);
            return;
        }

        if (message == null)
        {
            await MoveToDlqAsync(sourceQueue, rawMessage);
            return;
        }

        switch (message.Type.ToLowerInvariant())
        {
            case "audit":
                var auditRequest = JsonSerializer.Deserialize<CreateAuditLogRequest>(message.Payload);
                if (auditRequest != null)
                    await _auditLogService.CreateAsync(auditRequest, ct);
                break;

            case "notification":
                var notifRequest = JsonSerializer.Deserialize<DispatchNotificationRequest>(message.Payload);
                if (notifRequest != null)
                    await _notificationService.DispatchAsync(notifRequest, ct);
                break;

            default:
                _logger.LogWarning("Unknown outbox message type '{Type}' from {Queue}. Moving to DLQ.", message.Type, sourceQueue);
                await MoveToDlqAsync(sourceQueue, rawMessage);
                break;
        }
    }

    private async Task MoveToDlqAsync(string sourceQueue, string rawMessage)
    {
        var dlqKey = sourceQueue.Replace("outbox:", "dlq:");
        var db = _redis.GetDatabase();
        await db.ListLeftPushAsync(dlqKey, rawMessage);
    }
}
