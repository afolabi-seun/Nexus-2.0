using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.TimePolicies;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.TimePolicies;

public class TimePolicyRepository : GenericRepository<TimePolicy>, ITimePolicyRepository
{
    private readonly WorkDbContext _db;

    public TimePolicyRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<TimePolicy?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await _db.TimePolicies.FirstOrDefaultAsync(p => p.OrganizationId == organizationId, ct);
}
