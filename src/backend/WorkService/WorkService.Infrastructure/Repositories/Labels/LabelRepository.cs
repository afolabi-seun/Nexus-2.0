using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Labels;
using WorkService.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.Labels;

public class LabelRepository : ILabelRepository
{
    private readonly WorkDbContext _db;

    public LabelRepository(WorkDbContext db) => _db = db;

    public async Task<Label?> GetByIdAsync(Guid labelId, CancellationToken ct = default)
        => await _db.Labels.FirstOrDefaultAsync(l => l.LabelId == labelId, ct);

    public async Task<Label?> GetByNameAsync(Guid organizationId, string name, CancellationToken ct = default)
        => await _db.Labels.FirstOrDefaultAsync(l => l.OrganizationId == organizationId && l.Name == name, ct);

    public async Task<Label> AddAsync(Label label, CancellationToken ct = default)
    {
        _db.Labels.Add(label);
        await _db.SaveChangesAsync(ct);
        return label;
    }

    public async Task UpdateAsync(Label label, CancellationToken ct = default)
    {
        _db.Labels.Update(label);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Label label, CancellationToken ct = default)
    {
        _db.Labels.Remove(label);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Label>> ListAsync(Guid organizationId, CancellationToken ct = default)
        => await _db.Labels.Where(l => l.OrganizationId == organizationId).OrderBy(l => l.Name).ToListAsync(ct);
}
