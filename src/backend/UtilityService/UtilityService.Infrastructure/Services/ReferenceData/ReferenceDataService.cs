using System.Text.Json;
using StackExchange.Redis;
using UtilityService.Application.DTOs.ReferenceData;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Repositories.DepartmentTypes;
using UtilityService.Domain.Interfaces.Repositories.PriorityLevels;
using UtilityService.Domain.Interfaces.Repositories.TaskTypeRefs;
using UtilityService.Domain.Interfaces.Repositories.WorkflowStates;
using UtilityService.Domain.Interfaces.Services.ReferenceData;

namespace UtilityService.Infrastructure.Services.ReferenceData;

public class ReferenceDataService : IReferenceDataService
{
    private readonly IDepartmentTypeRepository _deptRepo;
    private readonly IPriorityLevelRepository _priorityRepo;
    private readonly ITaskTypeRefRepository _taskTypeRepo;
    private readonly IWorkflowStateRepository _workflowRepo;
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public ReferenceDataService(
        IDepartmentTypeRepository deptRepo, IPriorityLevelRepository priorityRepo,
        ITaskTypeRefRepository taskTypeRepo, IWorkflowStateRepository workflowRepo,
        IConnectionMultiplexer redis)
    {
        _deptRepo = deptRepo;
        _priorityRepo = priorityRepo;
        _taskTypeRepo = taskTypeRepo;
        _workflowRepo = workflowRepo;
        _redis = redis;
    }

    public async Task<IEnumerable<object>> GetDepartmentTypesAsync(CancellationToken ct = default)
    {
        return await GetCachedOrFetchAsync("ref:department_types", async () =>
        {
            var items = await _deptRepo.ListAsync(ct);
            return items.Select(e => new DepartmentTypeResponse
            {
                DepartmentTypeId = e.DepartmentTypeId, TypeName = e.TypeName, TypeCode = e.TypeCode
            }).ToList();
        });
    }

    public async Task<IEnumerable<object>> GetPriorityLevelsAsync(CancellationToken ct = default)
    {
        return await GetCachedOrFetchAsync("ref:priority_levels", async () =>
        {
            var items = await _priorityRepo.ListAsync(ct);
            return items.Select(e => new PriorityLevelResponse
            {
                PriorityLevelId = e.PriorityLevelId, Name = e.Name, SortOrder = e.SortOrder, Color = e.Color
            }).ToList();
        });
    }

    public async Task<IEnumerable<object>> GetTaskTypesAsync(CancellationToken ct = default)
    {
        return await GetCachedOrFetchAsync("ref:task_types", async () =>
        {
            var items = await _taskTypeRepo.ListAsync(ct);
            return items.Select(e => new TaskTypeRefResponse
            {
                TaskTypeRefId = e.TaskTypeRefId, TypeName = e.TypeName, DefaultDepartmentCode = e.DefaultDepartmentCode
            }).ToList();
        });
    }

    public async Task<IEnumerable<object>> GetWorkflowStatesAsync(CancellationToken ct = default)
    {
        return await GetCachedOrFetchAsync("ref:workflow_states", async () =>
        {
            var items = await _workflowRepo.ListAsync(ct);
            return items.Select(e => new WorkflowStateResponse
            {
                WorkflowStateId = e.WorkflowStateId, EntityType = e.EntityType, StateName = e.StateName, SortOrder = e.SortOrder
            }).ToList();
        });
    }

    public async Task<object> CreateDepartmentTypeAsync(object request, CancellationToken ct = default)
    {
        var req = (CreateDepartmentTypeRequest)request;
        if (await _deptRepo.ExistsAsync(req.TypeName, ct) || await _deptRepo.GetByCodeAsync(req.TypeCode, ct) != null)
            throw new ReferenceDataDuplicateException();

        var entity = new DepartmentType { TypeName = req.TypeName, TypeCode = req.TypeCode };
        var created = await _deptRepo.AddAsync(entity, ct);
        await InvalidateCacheAsync("ref:department_types");

        return new DepartmentTypeResponse
        {
            DepartmentTypeId = created.DepartmentTypeId, TypeName = created.TypeName, TypeCode = created.TypeCode
        };
    }

    public async Task<object> CreatePriorityLevelAsync(object request, CancellationToken ct = default)
    {
        var req = (CreatePriorityLevelRequest)request;
        if (await _priorityRepo.ExistsAsync(req.Name, ct))
            throw new ReferenceDataDuplicateException();

        var entity = new PriorityLevel { Name = req.Name, SortOrder = req.SortOrder, Color = req.Color };
        var created = await _priorityRepo.AddAsync(entity, ct);
        await InvalidateCacheAsync("ref:priority_levels");

        return new PriorityLevelResponse
        {
            PriorityLevelId = created.PriorityLevelId, Name = created.Name, SortOrder = created.SortOrder, Color = created.Color
        };
    }

    private async Task<List<T>> GetCachedOrFetchAsync<T>(string cacheKey, Func<Task<List<T>>> fetchFunc)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var result = JsonSerializer.Deserialize<List<T>>(cached!);
            if (result != null) return result;
        }

        var data = await fetchFunc();
        await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(data), CacheTtl);
        return data;
    }

    private async Task InvalidateCacheAsync(string cacheKey)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(cacheKey);
    }
}
