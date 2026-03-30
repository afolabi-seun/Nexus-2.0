using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WorkService.Application.DTOs.Search;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Domain.Interfaces.Services;
using StackExchange.Redis;

namespace WorkService.Infrastructure.Services.Search;

public class SearchService : ISearchService
{
    private readonly IStoryRepository _storyRepo;
    private readonly IConnectionMultiplexer _redis;

    public SearchService(IStoryRepository storyRepo, IConnectionMultiplexer redis)
    {
        _storyRepo = storyRepo;
        _redis = redis;
    }

    public async Task<object> SearchAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (SearchRequest)request;

        if (string.IsNullOrWhiteSpace(req.Query) || req.Query.Length < 2)
            throw new SearchQueryTooShortException();

        var db = _redis.GetDatabase();
        var cacheKey = $"search_results:{ComputeHash(organizationId, req)}";
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedResult = JsonSerializer.Deserialize<SearchResponse>(cached!);
            if (cachedResult != null) return cachedResult;
        }

        var (items, totalCount) = await _storyRepo.SearchAsync(organizationId, req.Query, req.Page, req.PageSize, ct);

        var response = new SearchResponse
        {
            TotalCount = totalCount, Page = req.Page, PageSize = req.PageSize,
            Items = items.Select(s => new SearchResultItem
            {
                Id = s.StoryId, EntityType = "Story", StoryKey = s.StoryKey,
                Title = s.Title, Status = s.Status, Priority = s.Priority
            }).ToList()
        };

        var json = JsonSerializer.Serialize(response);
        await db.StringSetAsync(cacheKey, json, TimeSpan.FromMinutes(1));

        return response;
    }

    private static string ComputeHash(Guid orgId, SearchRequest req)
    {
        var input = $"{orgId}:{req.Query}:{req.Page}:{req.PageSize}:{req.Status}:{req.Priority}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16];
    }
}
