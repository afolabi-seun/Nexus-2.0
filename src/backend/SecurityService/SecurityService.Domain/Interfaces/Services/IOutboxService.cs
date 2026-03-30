namespace SecurityService.Domain.Interfaces.Services;

public interface IOutboxService
{
    Task PublishAsync(string queueKey, string serializedMessage, CancellationToken ct = default);
}
