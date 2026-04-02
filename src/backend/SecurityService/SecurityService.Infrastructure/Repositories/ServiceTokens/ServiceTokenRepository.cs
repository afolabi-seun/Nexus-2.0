using SecurityService.Domain.Entities;
using SecurityService.Domain.Interfaces.Repositories.ServiceTokens;
using SecurityService.Infrastructure.Data;
using SecurityService.Infrastructure.Repositories.Generics;

namespace SecurityService.Infrastructure.Repositories.ServiceTokens;

public class ServiceTokenRepository : GenericRepository<ServiceToken>, IServiceTokenRepository
{
    private readonly SecurityDbContext _db;

    public ServiceTokenRepository(SecurityDbContext db) : base(db)
    {
        _db = db;
    }
}
