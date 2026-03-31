namespace SecurityService.Domain.Interfaces.Services.Outbox;

public interface IOutboxService
{
    Task PublishAsync(string queueKey, string serializedMessage, CancellationToken ct = default);
}
