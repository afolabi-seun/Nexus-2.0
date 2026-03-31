using System.Text.Json;
using StackExchange.Redis;
using UtilityService.Application.DTOs.ErrorCodes;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Repositories.ErrorCodeEntries;
using UtilityService.Domain.Interfaces.Services.ErrorCodes;

namespace UtilityService.Infrastructure.Services.ErrorCodes;

public class ErrorCodeService : IErrorCodeService
{
    private readonly IErrorCodeEntryRepository _repo;
    private readonly IConnectionMultiplexer _redis;
    private const string CacheKey = "error_codes_registry";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public ErrorCodeService(IErrorCodeEntryRepository repo, IConnectionMultiplexer redis)
    {
        _repo = repo;
        _redis = redis;
    }

    public async Task<object> CreateAsync(object request, CancellationToken ct = default)
    {
        var req = (CreateErrorCodeRequest)request;
        var existing = await _repo.GetByCodeAsync(req.Code, ct);
        if (existing != null)
            throw new ErrorCodeDuplicateException(req.Code);

        var entity = new ErrorCodeEntry
        {
            Code = req.Code, Value = req.Value, HttpStatusCode = req.HttpStatusCode,
            ResponseCode = req.ResponseCode, Description = req.Description,
            ServiceName = req.ServiceName
        };

        var created = await _repo.AddAsync(entity, ct);
        await InvalidateCacheAsync();
        return MapToResponse(created);
    }

    public async Task<IEnumerable<object>> ListAsync(CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(CacheKey);
        if (cached.HasValue)
        {
            var cachedList = JsonSerializer.Deserialize<List<ErrorCodeResponse>>(cached!);
            if (cachedList != null) return cachedList;
        }

        var items = await _repo.ListAsync(ct);
        var responses = items.Select(MapToResponse).ToList();
        await db.StringSetAsync(CacheKey, JsonSerializer.Serialize(responses), CacheTtl);
        return responses;
    }

    public async Task<object> UpdateAsync(string code, object request, CancellationToken ct = default)
    {
        var req = (UpdateErrorCodeRequest)request;
        var entity = await _repo.GetByCodeAsync(code, ct)
            ?? throw new ErrorCodeNotFoundException(code);

        if (req.Value.HasValue) entity.Value = req.Value.Value;
        if (req.HttpStatusCode.HasValue) entity.HttpStatusCode = req.HttpStatusCode.Value;
        if (req.ResponseCode != null) entity.ResponseCode = req.ResponseCode;
        if (req.Description != null) entity.Description = req.Description;
        if (req.ServiceName != null) entity.ServiceName = req.ServiceName;
        entity.DateUpdated = DateTime.UtcNow;

        await _repo.UpdateAsync(entity, ct);
        await InvalidateCacheAsync();
        return MapToResponse(entity);
    }

    public async Task DeleteAsync(string code, CancellationToken ct = default)
    {
        var entity = await _repo.GetByCodeAsync(code, ct)
            ?? throw new ErrorCodeNotFoundException(code);

        await _repo.RemoveAsync(entity, ct);
        await InvalidateCacheAsync();
    }

    private async Task InvalidateCacheAsync()
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(CacheKey);
    }

    private static ErrorCodeResponse MapToResponse(ErrorCodeEntry e) => new()
    {
        ErrorCodeEntryId = e.ErrorCodeEntryId, Code = e.Code, Value = e.Value,
        HttpStatusCode = e.HttpStatusCode, ResponseCode = e.ResponseCode,
        Description = e.Description, ServiceName = e.ServiceName,
        DateCreated = e.DateCreated, DateUpdated = e.DateUpdated
    };
}
