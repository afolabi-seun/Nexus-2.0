namespace ProfileService.Domain.Interfaces.Services.Outbox;

public interface IOutboxService
{
    Task PublishAsync(object message, CancellationToken ct = default);
}
