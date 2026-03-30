namespace ProfileService.Infrastructure.Services.ServiceClients;

public interface ISecurityServiceClient
{
    Task GenerateCredentialsAsync(Guid memberId, string email, CancellationToken ct = default);
}
