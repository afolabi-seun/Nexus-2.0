using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Labels;
using WorkService.Infrastructure.Data;
using WorkService.Infrastructure.Repositories.Generics;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.Labels;

public class LabelRepository : GenericRepository<Label>, ILabelRepository
{
    private readonly WorkDbContext _db;

    public LabelRepository(WorkDbContext db) : base(db)
    {
        _db = db;
    }

    public async Task<Label?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default)
        => await _db.Labels.FirstOrDefaultAsync(l => l.OrganizationId == organizationId && l.Name == name, ct);

    public async Task<IEnumerable<Label>> ListAsync(Guid organizationId, CancellationToken ct = default)
        => await _db.Labels.Where(l => l.OrganizationId == organizationId).OrderBy(l => l.Name).ToListAsync(ct);
}
