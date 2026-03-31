using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.TimePolicies;
using WorkService.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.TimePolicies;

public class TimePolicyRepository : ITimePolicyRepository
{
    private readonly WorkDbContext _db;

    public TimePolicyRepository(WorkDbContext db) => _db = db;

    public async Task<TimePolicy?> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
        => await _db.TimePolicies.FirstOrDefaultAsync(p => p.OrganizationId == organizationId, ct);

    public async Task<TimePolicy> AddAsync(TimePolicy policy, CancellationToken ct = default)
    {
        _db.TimePolicies.Add(policy);
        await _db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task UpdateAsync(TimePolicy policy, CancellationToken ct = default)
    {
        _db.TimePolicies.Update(policy);
        await _db.SaveChangesAsync(ct);
    }
}
