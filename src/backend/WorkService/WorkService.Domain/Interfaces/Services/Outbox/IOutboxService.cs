namespace WorkService.Domain.Interfaces.Services;

public interface IOutboxService
{
    Task PublishAsync(object message, CancellationToken ct = default);
}
