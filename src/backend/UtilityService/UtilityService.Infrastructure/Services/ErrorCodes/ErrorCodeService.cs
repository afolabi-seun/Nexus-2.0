using UtilityService.Domain.Results;
using System.Text.Json;
using StackExchange.Redis;
using UtilityService.Application.DTOs;
using UtilityService.Application.DTOs.ErrorCodes;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Repositories.ErrorCodeEntries;
using UtilityService.Domain.Interfaces.Services.ErrorCodes;
using UtilityService.Infrastructure.Data;
using UtilityService.Infrastructure.Redis;

namespace UtilityService.Infrastructure.Services.ErrorCodes;

public class ErrorCodeService : IErrorCodeService
{
    private readonly IErrorCodeEntryRepository _repo;
    private readonly IConnectionMultiplexer _redis;
    private readonly UtilityDbContext _dbContext;
    private static readonly string CacheKey = RedisKeys.ErrorCodesRegistry;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public ErrorCodeService(IErrorCodeEntryRepository repo, IConnectionMultiplexer redis, UtilityDbContext dbContext)
    {
        _repo = repo;
        _redis = redis;
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<object>> CreateAsync(object request, CancellationToken ct = default)
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
        await _dbContext.SaveChangesAsync(ct);
        await InvalidateCacheAsync();
        return ServiceResult<object>.Created(MapToResponse(created), "Error code created.");
    }

    public async Task<ServiceResult<IEnumerable<object>>> ListAsync(CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(CacheKey);
        if (cached.HasValue)
        {
            var cachedList = JsonSerializer.Deserialize<List<ErrorCodeResponse>>(cached!);
            if (cachedList != null) return ServiceResult<IEnumerable<object>>.Ok(cachedList, "Error codes retrieved.");
        }

        var items = await _repo.ListAsync(ct);
        var responses = items.Select(MapToResponse).ToList();
        await db.StringSetAsync(CacheKey, JsonSerializer.Serialize(responses), CacheTtl);
        return ServiceResult<IEnumerable<object>>.Ok(responses, "Error codes retrieved.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(string code, object request, CancellationToken ct = default)
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
        await _dbContext.SaveChangesAsync(ct);
        await InvalidateCacheAsync();
        return ServiceResult<object>.Ok(MapToResponse(entity), "Error code updated.");
    }

    public async Task<ServiceResult<object>> DeleteAsync(string code, CancellationToken ct = default)
    {
        var entity = await _repo.GetByCodeAsync(code, ct)
            ?? throw new ErrorCodeNotFoundException(code);

        await _repo.RemoveAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        await InvalidateCacheAsync();
        return ServiceResult<object>.NoContent("Error code deleted.");
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
