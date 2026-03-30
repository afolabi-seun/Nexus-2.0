namespace UtilityService.Domain.Interfaces.Services;

public interface IOutboxMessageRouter
{
    Task RouteAsync(string rawMessage, string sourceQueue, CancellationToken ct = default);
}
