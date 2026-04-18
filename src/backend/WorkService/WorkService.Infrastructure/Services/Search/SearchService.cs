using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WorkService.Application.DTOs.Search;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.Search;
using StackExchange.Redis;
using WorkService.Infrastructure.Redis;

namespace WorkService.Infrastructure.Services.Search;

public class SearchService : ISearchService
{
    private readonly IStoryRepository _storyRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ITaskRepository _taskRepo;
    private readonly IConnectionMultiplexer _redis;

    public SearchService(IStoryRepository storyRepo, IProjectRepository projectRepo,
        ITaskRepository taskRepo, IConnectionMultiplexer redis)
    {
        _storyRepo = storyRepo;
        _projectRepo = projectRepo;
        _taskRepo = taskRepo;
        _redis = redis;
    }

    public async Task<object> SearchAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (SearchRequest)request;

        if (string.IsNullOrWhiteSpace(req.Query) || req.Query.Length < 2)
            throw new SearchQueryTooShortException();

        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.SearchResults(ComputeHash(organizationId, req));
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedResult = JsonSerializer.Deserialize<SearchResponse>(cached!);
            if (cachedResult != null) return cachedResult;
        }

        var items = new List<SearchResultItem>();
        var totalCount = 0;
        var entityFilter = req.EntityType?.ToLowerInvariant();

        // Search stories
        if (entityFilter is null or "story")
        {
            var (stories, storyCount) = await _storyRepo.SearchAsync(organizationId, req.Query, req.Page, req.PageSize, ct);
            totalCount += storyCount;
            items.AddRange(stories.Select(s => new SearchResultItem
            {
                Id = s.StoryId, EntityType = "Story", StoryKey = s.StoryKey,
                Title = s.Title, Status = s.Status, Priority = s.Priority
            }));
        }

        // Search projects
        if (entityFilter is null or "project")
        {
            var (projects, projectCount) = await _projectRepo.SearchAsync(organizationId, req.Query, req.Page, req.PageSize, ct);
            totalCount += projectCount;
            items.AddRange(projects.Select(p => new SearchResultItem
            {
                Id = p.ProjectId, EntityType = "Project",
                Title = p.ProjectName, Status = p.FlgStatus == "A" ? "Active" : "Inactive"
            }));
        }

        // Search tasks
        if (entityFilter is null or "task")
        {
            var (tasks, taskCount) = await _taskRepo.SearchAsync(organizationId, req.Query, req.Page, req.PageSize, ct);
            totalCount += taskCount;
            items.AddRange(tasks.Select(t => new SearchResultItem
            {
                Id = t.TaskId, EntityType = "Task",
                Title = t.Title, Status = t.Status, Priority = t.Priority
            }));
        }

        var response = new SearchResponse
        {
            TotalCount = totalCount, Page = req.Page, PageSize = req.PageSize,
            Items = items
        };

        var json = JsonSerializer.Serialize(response);
        await db.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(1));

        return response;
    }

    private static string ComputeHash(Guid orgId, SearchRequest req)
    {
        var input = $"{orgId}:{req.Query}:{req.Page}:{req.PageSize}:{req.Status}:{req.Priority}:{req.EntityType}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16];
    }
}
