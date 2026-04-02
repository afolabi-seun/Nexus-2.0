using SecurityService.Domain.Entities;
using SecurityService.Domain.Interfaces.Repositories.Generics;

namespace SecurityService.Domain.Interfaces.Repositories.ServiceTokens;

public interface IServiceTokenRepository : IGenericRepository<ServiceToken>
{
}
