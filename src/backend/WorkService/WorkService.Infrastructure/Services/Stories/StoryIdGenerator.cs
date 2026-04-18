using Microsoft.Extensions.Logging;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.StorySequences;
using WorkService.Domain.Interfaces.Services.Stories;
using StackExchange.Redis;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.Stories;

public class StoryIdGenerator : IStoryIdGenerator
{
    private readonly IStorySequenceRepository _sequenceRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StoryIdGenerator> _logger;

    public StoryIdGenerator(
        IStorySequenceRepository sequenceRepo,
        IProjectRepository projectRepo,
        IConnectionMultiplexer redis,
        ILogger<StoryIdGenerator> logger)
    {
        _sequenceRepo = sequenceRepo;
        _projectRepo = projectRepo;
        _redis = redis;
        _logger = logger;
    }

    public async Task<(string StoryKey, long SequenceNumber)> GenerateNextIdAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var projectKey = await GetProjectKeyAsync(projectId, ct);

        await _sequenceRepo.InitializeAsync(projectId, ct);

        var nextVal = await _sequenceRepo.IncrementAndGetAsync(projectId, ct);

        return ($"{projectKey}-{nextVal}", nextVal);
    }

    private async Task<string> GetProjectKeyAsync(Guid projectId, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(RedisKeys.ProjectPrefix(projectId));
        if (cached.HasValue) return cached.ToString();

        var project = await _projectRepo.GetByIdAsync(projectId, ct)
            ?? throw new ProjectNotFoundException(projectId);

        await db.StringSetAsync(
            RedisKeys.ProjectPrefix(projectId),
            project.ProjectKey,
            TimeSpan.FromMinutes(60));

        return project.ProjectKey;
    }
}
