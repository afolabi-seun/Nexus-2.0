namespace UtilityService.Domain.Interfaces.Services.Outbox;

public interface IOutboxMessageRouter
{
    Task RouteAsync(string rawMessage, string sourceQueue, CancellationToken ct = default);
}
