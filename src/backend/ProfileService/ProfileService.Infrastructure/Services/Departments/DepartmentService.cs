using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Departments;
using ProfileService.Application.DTOs.TeamMembers;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Domain.Interfaces.Services.Departments;
using ProfileService.Domain.Results;
using ProfileService.Infrastructure.Data;
using StackExchange.Redis;
using ProfileService.Infrastructure.Redis;

namespace ProfileService.Infrastructure.Services.Departments;

public class DepartmentService : IDepartmentService
{
    private static readonly TimeSpan DeptListCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DeptPrefsCacheTtl = TimeSpan.FromMinutes(10);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IDepartmentRepository _deptRepo;
    private readonly IDepartmentMemberRepository _deptMemberRepo;
    private readonly ITeamMemberRepository _memberRepo;
    private readonly IConnectionMultiplexer _redis;
    private readonly ProfileDbContext _dbContext;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(
        IDepartmentRepository deptRepo,
        IDepartmentMemberRepository deptMemberRepo,
        ITeamMemberRepository memberRepo,
        IConnectionMultiplexer redis,
        ProfileDbContext dbContext,
        ILogger<DepartmentService> logger)
    {
        _deptRepo = deptRepo;
        _deptMemberRepo = deptMemberRepo;
        _memberRepo = memberRepo;
        _redis = redis;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (CreateDepartmentRequest)request;

        // Validate name uniqueness within org
        var existingByName = await _deptRepo.GetByNameAsync(organizationId, req.DepartmentName, ct);
        if (existingByName is not null)
            throw new DepartmentNameDuplicateException();

        // Validate code uniqueness within org
        var existingByCode = await _deptRepo.GetByCodeAsync(organizationId, req.DepartmentCode, ct);
        if (existingByCode is not null)
            throw new DepartmentCodeDuplicateException();

        var dept = new Department
        {
            OrganizationId = organizationId,
            DepartmentName = req.DepartmentName,
            DepartmentCode = req.DepartmentCode,
            Description = req.Description,
            IsDefault = false,
            FlgStatus = EntityStatuses.Active
        };

        await _deptRepo.AddAsync(dept, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeys.DeptList(organizationId));

        return ServiceResult<object>.Created(MapToResponse(dept, 0), "Department created successfully.");
    }

    public async Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cacheKey = RedisKeys.DeptListPaged(organizationId, page, pageSize);

        var cached = await db.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            var cachedResult = JsonSerializer.Deserialize<PaginatedResponse<DepartmentResponse>>(cached!, JsonOptions);
            if (cachedResult is not null)
                return ServiceResult<object>.Ok(cachedResult, "Departments retrieved.");
        }

        var (items, totalCount) = await _deptRepo.ListByOrganizationAsync(organizationId, page, pageSize, ct);
        var responses = new List<DepartmentResponse>();
        foreach (var dept in items)
        {
            var memberCount = await _deptRepo.GetActiveMemberCountAsync(dept.DepartmentId, ct);
            responses.Add(MapToResponse(dept, memberCount));
        }

        var result = new PaginatedResponse<DepartmentResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);
        await db.StringSetAsync(cacheKey, json, DeptListCacheTtl);

        return ServiceResult<object>.Ok(result, "Departments retrieved.");
    }

    public async Task<ServiceResult<object>> GetByIdAsync(Guid departmentId, CancellationToken ct = default)
    {
        var dept = await _deptRepo.GetByIdAsync(departmentId, ct)
            ?? throw new DepartmentNotFoundException($"Department {departmentId} not found");

        var memberCount = await _deptRepo.GetActiveMemberCountAsync(departmentId, ct);
        return ServiceResult<object>.Ok(MapToResponse(dept, memberCount), "Department retrieved.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid departmentId, object request, CancellationToken ct = default)
    {
        var req = (UpdateDepartmentRequest)request;
        var dept = await _deptRepo.GetByIdAsync(departmentId, ct)
            ?? throw new DepartmentNotFoundException($"Department {departmentId} not found");

        // Validate name uniqueness if changed
        if (req.DepartmentName is not null && req.DepartmentName != dept.DepartmentName)
        {
            var existing = await _deptRepo.GetByNameAsync(dept.OrganizationId, req.DepartmentName, ct);
            if (existing is not null)
                throw new DepartmentNameDuplicateException();
            dept.DepartmentName = req.DepartmentName;
        }

        if (req.Description is not null) dept.Description = req.Description;

        dept.DateUpdated = DateTime.UtcNow;
        await _deptRepo.UpdateAsync(dept, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeys.DeptList(dept.OrganizationId));

        var memberCount = await _deptRepo.GetActiveMemberCountAsync(departmentId, ct);
        return ServiceResult<object>.Ok(MapToResponse(dept, memberCount), "Department updated.");
    }

    public async Task<ServiceResult<object>> UpdateStatusAsync(Guid departmentId, string newStatus, CancellationToken ct = default)
    {
        var dept = await _deptRepo.GetByIdAsync(departmentId, ct)
            ?? throw new DepartmentNotFoundException($"Department {departmentId} not found");

        // Protect default departments
        if (dept.IsDefault && newStatus == EntityStatuses.Deactivated)
            throw new DefaultDepartmentCannotDeleteException();

        // Check for active members before deactivation
        if (newStatus == EntityStatuses.Deactivated)
        {
            var activeCount = await _deptRepo.GetActiveMemberCountAsync(departmentId, ct);
            if (activeCount > 0)
                throw new DepartmentHasActiveMembersException();
        }

        dept.FlgStatus = newStatus;
        dept.DateUpdated = DateTime.UtcNow;
        await _deptRepo.UpdateAsync(dept, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeys.DeptList(dept.OrganizationId));

        return ServiceResult<object>.Ok(null!, "Department status updated.");
    }

    public async Task<ServiceResult<object>> ListMembersAsync(Guid departmentId, int page, int pageSize, CancellationToken ct = default)
    {
        var dept = await _deptRepo.GetByIdAsync(departmentId, ct)
            ?? throw new DepartmentNotFoundException($"Department {departmentId} not found");

        var (items, totalCount) = await _deptMemberRepo.ListByDepartmentAsync(departmentId, page, pageSize, ct);
        var responses = new List<TeamMemberResponse>();

        foreach (var dm in items)
        {
            var member = dm.TeamMember ?? await _memberRepo.GetByIdAsync(dm.TeamMemberId, ct);
            if (member is not null)
            {
                responses.Add(new TeamMemberResponse
                {
                    TeamMemberId = member.TeamMemberId,
                    Email = member.Email,
                    FirstName = member.FirstName,
                    LastName = member.LastName,
                    DisplayName = member.DisplayName,
                    AvatarUrl = member.AvatarUrl,
                    Title = member.Title,
                    ProfessionalId = member.ProfessionalId,
                    Availability = member.Availability,
                    FlgStatus = member.FlgStatus
                });
            }
        }

        return ServiceResult<object>.Ok(new PaginatedResponse<TeamMemberResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        }, "Department members retrieved.");
    }

    public async Task<ServiceResult<object>> GetPreferencesAsync(Guid departmentId, CancellationToken ct = default)
    {
        var dept = await _deptRepo.GetByIdAsync(departmentId, ct)
            ?? throw new DepartmentNotFoundException($"Department {departmentId} not found");

        var prefs = !string.IsNullOrEmpty(dept.PreferencesJson)
            ? JsonSerializer.Deserialize<DepartmentPreferences>(dept.PreferencesJson, JsonOptions) ?? new DepartmentPreferences()
            : new DepartmentPreferences();

        return ServiceResult<object>.Ok(MapPrefsToResponse(prefs), "Department preferences retrieved.");
    }

    public async Task<ServiceResult<object>> UpdatePreferencesAsync(Guid departmentId, object request, CancellationToken ct = default)
    {
        var req = (DepartmentPreferencesRequest)request;
        var dept = await _deptRepo.GetByIdAsync(departmentId, ct)
            ?? throw new DepartmentNotFoundException($"Department {departmentId} not found");

        var prefs = !string.IsNullOrEmpty(dept.PreferencesJson)
            ? JsonSerializer.Deserialize<DepartmentPreferences>(dept.PreferencesJson, JsonOptions) ?? new DepartmentPreferences()
            : new DepartmentPreferences();

        if (req.DefaultTaskTypes is not null) prefs.DefaultTaskTypes = req.DefaultTaskTypes;
        if (req.CustomWorkflowOverrides is not null) prefs.CustomWorkflowOverrides = req.CustomWorkflowOverrides;
        if (req.WipLimitPerStatus is not null) prefs.WipLimitPerStatus = req.WipLimitPerStatus;
        if (req.DefaultAssigneeId.HasValue) prefs.DefaultAssigneeId = req.DefaultAssigneeId;
        if (req.NotificationChannelOverrides is not null) prefs.NotificationChannelOverrides = req.NotificationChannelOverrides;
        if (req.MaxConcurrentTasksDefault.HasValue) prefs.MaxConcurrentTasksDefault = req.MaxConcurrentTasksDefault.Value;

        dept.PreferencesJson = JsonSerializer.Serialize(prefs, JsonOptions);
        dept.DateUpdated = DateTime.UtcNow;
        await _deptRepo.UpdateAsync(dept, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKeys.DeptPrefs(departmentId));

        return ServiceResult<object>.Ok(MapPrefsToResponse(prefs), "Department preferences updated.");
    }

    private static DepartmentResponse MapToResponse(Department dept, int memberCount) => new()
    {
        DepartmentId = dept.DepartmentId,
        DepartmentName = dept.DepartmentName,
        DepartmentCode = dept.DepartmentCode,
        Description = dept.Description,
        IsDefault = dept.IsDefault,
        FlgStatus = dept.FlgStatus,
        MemberCount = memberCount,
        DateCreated = dept.DateCreated,
        DateUpdated = dept.DateUpdated
    };

    private static DepartmentPreferencesResponse MapPrefsToResponse(DepartmentPreferences p) => new()
    {
        DefaultTaskTypes = p.DefaultTaskTypes,
        CustomWorkflowOverrides = p.CustomWorkflowOverrides,
        WipLimitPerStatus = p.WipLimitPerStatus,
        DefaultAssigneeId = p.DefaultAssigneeId,
        NotificationChannelOverrides = p.NotificationChannelOverrides,
        MaxConcurrentTasksDefault = p.MaxConcurrentTasksDefault
    };
}
