using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.AuditLogs;
using UtilityService.Application.DTOs.Notifications;
using UtilityService.Domain.Interfaces.Services;
using UtilityService.Infrastructure.Services.Outbox;

namespace UtilityService.Tests.Services;

public class OutboxMessageRouterTests
{
    private readonly Mock<IAuditLogService> _auditMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly Mock<ILogger<OutboxMessageRouter>> _loggerMock = new();
    private readonly OutboxMessageRouter _sut;

    public OutboxMessageRouterTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        _dbMock.Setup(d => d.ListLeftPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        _sut = new OutboxMessageRouter(_auditMock.Object, _notifMock.Object, _redisMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RouteAsync_AuditType_RoutesToAuditLogService()
    {
        var auditPayload = new CreateAuditLogRequest
        {
            OrganizationId = Guid.NewGuid(), ServiceName = "TestSvc", Action = "Create",
            EntityType = "User", EntityId = "1", UserId = "u1", CorrelationId = "c1"
        };
        var message = new OutboxMessage
        {
            Type = "audit",
            Payload = JsonSerializer.Serialize(auditPayload)
        };

        await _sut.RouteAsync(JsonSerializer.Serialize(message), "outbox:profile");

        _auditMock.Verify(a => a.CreateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.DispatchAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteAsync_NotificationType_RoutesToNotificationService()
    {
        var notifPayload = new DispatchNotificationRequest
        {
            OrganizationId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            NotificationType = "StoryAssigned", Channels = "Email", Recipient = "u@t.com"
        };
        var message = new OutboxMessage
        {
            Type = "notification",
            Payload = JsonSerializer.Serialize(notifPayload)
        };

        await _sut.RouteAsync(JsonSerializer.Serialize(message), "outbox:work");

        _notifMock.Verify(n => n.DispatchAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.Verify(a => a.CreateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteAsync_UnknownType_MovesToDlq()
    {
        var message = new OutboxMessage { Type = "unknown", Payload = "{}" };
        var raw = JsonSerializer.Serialize(message);

        await _sut.RouteAsync(raw, "outbox:security");

        _dbMock.Verify(d => d.ListLeftPushAsync(
            (RedisKey)"dlq:security",
            (RedisValue)raw,
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
        _auditMock.Verify(a => a.CreateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        _notifMock.Verify(n => n.DispatchAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteAsync_InvalidJson_MovesToDlq()
    {
        var raw = "not valid json {{{";

        await _sut.RouteAsync(raw, "outbox:profile");

        _dbMock.Verify(d => d.ListLeftPushAsync(
            (RedisKey)"dlq:profile",
            (RedisValue)raw,
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
}
