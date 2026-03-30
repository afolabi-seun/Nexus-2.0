using Microsoft.EntityFrameworkCore;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Repositories.StorySequences;

public class StorySequenceRepository : IStorySequenceRepository
{
    private readonly WorkDbContext _db;

    public StorySequenceRepository(WorkDbContext db) => _db = db;

    public async Task InitializeAsync(Guid projectId, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"StorySequences\" (\"ProjectId\", \"CurrentValue\") VALUES ({0}, 0) ON CONFLICT DO NOTHING",
            new object[] { projectId });
    }

    public async Task<long> IncrementAndGetAsync(Guid projectId, CancellationToken ct = default)
    {
        var result = await _db.Database
            .SqlQueryRaw<long>(
                "UPDATE \"StorySequences\" SET \"CurrentValue\" = \"CurrentValue\" + 1 WHERE \"ProjectId\" = {0} RETURNING \"CurrentValue\"",
                projectId)
            .FirstAsync(ct);

        return result;
    }
}
