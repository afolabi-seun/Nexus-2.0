using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.TeamMembers;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.Roles;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Domain.Interfaces.Services.TeamMembers;
using ProfileService.Infrastructure.Data;
using StackExchange.Redis;

namespace ProfileService.Infrastructure.Services.TeamMembers;

public class TeamMemberService : ITeamMemberService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ITeamMemberRepository _memberRepo;
    private readonly IDepartmentMemberRepository _deptMemberRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IConnectionMultiplexer _redis;
    private readonly ProfileDbContext _dbContext;
    private readonly ILogger<TeamMemberService> _logger;

    public TeamMemberService(
        ITeamMemberRepository memberRepo,
        IDepartmentMemberRepository deptMemberRepo,
        IDepartmentRepository deptRepo,
        IRoleRepository roleRepo,
        IConnectionMultiplexer redis,
        ProfileDbContext dbContext,
        ILogger<TeamMemberService> logger)
    {
        _memberRepo = memberRepo;
        _deptMemberRepo = deptMemberRepo;
        _deptRepo = deptRepo;
        _roleRepo = roleRepo;
        _redis = redis;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<object> ListAsync(Guid organizationId, int page, int pageSize,
        string? departmentId, string? role, string? status, string? availability,
        CancellationToken ct = default)
    {
        Guid? deptGuid = !string.IsNullOrEmpty(departmentId) && Guid.TryParse(departmentId, out var parsed)
            ? parsed : null;

        var (items, totalCount) = await _memberRepo.ListAsync(
            organizationId, page, pageSize, deptGuid, role, status, availability, ct);

        return new PaginatedResponse<TeamMemberResponse>
        {
            Data = items.Select(MapToResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<object> GetByIdAsync(Guid memberId, CancellationToken ct = default)
    {
        var member = await _memberRepo.GetByIdAsync(memberId, ct)
            ?? throw new MemberNotFoundException($"Member {memberId} not found");

        var memberships = await _deptMemberRepo.GetByMemberIdAsync(memberId, ct);
        return MapToDetailResponse(member, memberships);
    }

    public async Task<object> UpdateAsync(Guid memberId, object request, CancellationToken ct = default)
    {
        var req = (UpdateTeamMemberRequest)request;
        var member = await _memberRepo.GetByIdAsync(memberId, ct)
            ?? throw new MemberNotFoundException($"Member {memberId} not found");

        if (req.FirstName is not null) member.FirstName = req.FirstName;
        if (req.LastName is not null) member.LastName = req.LastName;
        if (req.DisplayName is not null) member.DisplayName = req.DisplayName;
        if (req.AvatarUrl is not null) member.AvatarUrl = req.AvatarUrl;
        if (req.Title is not null) member.Title = req.Title;
        if (req.Skills is not null) member.Skills = JsonSerializer.Serialize(req.Skills, JsonOptions);
        if (req.MaxConcurrentTasks.HasValue) member.MaxConcurrentTasks = req.MaxConcurrentTasks.Value;

        member.DateUpdated = DateTime.UtcNow;
        await _memberRepo.UpdateAsync(member, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"member_profile:{memberId}");

        var memberships = await _deptMemberRepo.GetByMemberIdAsync(memberId, ct);
        return MapToDetailResponse(member, memberships);
    }

    public async Task UpdateStatusAsync(Guid memberId, string newStatus, CancellationToken ct = default)
    {
        var member = await _memberRepo.GetByIdAsync(memberId, ct)
            ?? throw new MemberNotFoundException($"Member {memberId} not found");

        // Last OrgAdmin guard
        if (newStatus != EntityStatuses.Active)
        {
            var memberships = await _deptMemberRepo.GetByMemberIdAsync(memberId, ct);
            var isOrgAdmin = false;
            foreach (var dm in memberships)
            {
                var role = dm.Role ?? await _roleRepo.GetByIdAsync(dm.RoleId, ct);
                if (role?.RoleName == RoleNames.OrgAdmin)
                {
                    isOrgAdmin = true;
                    break;
                }
            }

            if (isOrgAdmin)
            {
                var adminCount = await _memberRepo.CountOrgAdminsAsync(member.OrganizationId, ct);
                if (adminCount <= 1)
                    throw new LastOrgAdminCannotDeactivateException();
            }
        }

        member.FlgStatus = newStatus;
        member.DateUpdated = DateTime.UtcNow;
        await _memberRepo.UpdateAsync(member, ct);
        await _dbContext.SaveChangesAsync(ct);

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"member_profile:{memberId}");
    }

    public async Task UpdateAvailabilityAsync(Guid memberId, string availability, CancellationToken ct = default)
    {
        var validValues = new[] { "Available", "Busy", "Away", "Offline" };
        if (!validValues.Contains(availability))
            throw new InvalidAvailabilityStatusException();

        var member = await _memberRepo.GetByIdAsync(memberId, ct)
            ?? throw new MemberNotFoundException($"Member {memberId} not found");

        member.Availability = availability;
        member.DateUpdated = DateTime.UtcNow;
        await _memberRepo.UpdateAsync(member, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task AddToDepartmentAsync(Guid memberId, object request, CancellationToken ct = default)
    {
        var req = (AddDepartmentRequest)request;
        var member = await _memberRepo.GetByIdAsync(memberId, ct)
            ?? throw new MemberNotFoundException($"Member {memberId} not found");

        // Check duplicate
        var existing = await _deptMemberRepo.GetAsync(memberId, req.DepartmentId, ct);
        if (existing is not null)
            throw new MemberAlreadyInDepartmentException();

        var dept = await _deptRepo.GetByIdAsync(req.DepartmentId, ct)
            ?? throw new DepartmentNotFoundException($"Department {req.DepartmentId} not found");

        var deptMember = new DepartmentMember
        {
            TeamMemberId = memberId,
            DepartmentId = req.DepartmentId,
            OrganizationId = member.OrganizationId,
            RoleId = req.RoleId
        };
        await _deptMemberRepo.AddAsync(deptMember, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate caches
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"member_profile:{memberId}");
        await db.KeyDeleteAsync($"dept_list:{member.OrganizationId}");
    }

    public async Task RemoveFromDepartmentAsync(Guid memberId, Guid departmentId, CancellationToken ct = default)
    {
        var member = await _memberRepo.GetByIdAsync(memberId, ct)
            ?? throw new MemberNotFoundException($"Member {memberId} not found");

        var deptMember = await _deptMemberRepo.GetAsync(memberId, departmentId, ct)
            ?? throw new MemberNotInDepartmentException();

        // Last department guard
        var allMemberships = await _deptMemberRepo.GetByMemberIdAsync(memberId, ct);
        if (allMemberships.Count() <= 1)
            throw new MemberMustHaveDepartmentException();

        await _deptMemberRepo.DeleteAsync(deptMember, ct);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate caches
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"member_profile:{memberId}");
        await db.KeyDeleteAsync($"dept_list:{member.OrganizationId}");
    }

    public async Task ChangeDepartmentRoleAsync(Guid memberId, Guid departmentId, object request, CancellationToken ct = default)
    {
        var req = (ChangeRoleRequest)request;
        var deptMember = await _deptMemberRepo.GetAsync(memberId, departmentId, ct)
            ?? throw new MemberNotInDepartmentException();

        deptMember.RoleId = req.RoleId;
        await _deptMemberRepo.UpdateAsync(deptMember, ct);
        await _dbContext.SaveChangesAsync(ct);

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"member_profile:{memberId}");
    }

    public async Task<object> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var member = await _memberRepo.GetByEmailGlobalAsync(email, ct)
            ?? throw new MemberNotFoundException($"Member with email not found");

        // Get the highest-level role from memberships
        var roleName = RoleNames.Member;
        if (member.DepartmentMemberships?.Any() == true)
        {
            var highestRole = member.DepartmentMemberships
                .Where(dm => dm.Role is not null)
                .OrderByDescending(dm => dm.Role!.PermissionLevel)
                .FirstOrDefault();
            if (highestRole?.Role is not null)
                roleName = highestRole.Role.RoleName;
        }

        return new TeamMemberInternalResponse
        {
            TeamMemberId = member.TeamMemberId,
            PasswordHash = member.Password,
            FlgStatus = member.FlgStatus,
            IsFirstTimeUser = member.IsFirstTimeUser,
            OrganizationId = member.OrganizationId,
            PrimaryDepartmentId = member.PrimaryDepartmentId,
            RoleName = roleName
        };
    }

    public async Task UpdatePasswordAsync(Guid memberId, string passwordHash, CancellationToken ct = default)
    {
        var member = await _memberRepo.GetByIdAsync(memberId, ct)
            ?? throw new MemberNotFoundException($"Member {memberId} not found");

        member.Password = passwordHash;
        member.DateUpdated = DateTime.UtcNow;
        await _memberRepo.UpdateAsync(member, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    private static TeamMemberResponse MapToResponse(TeamMember m) => new()
    {
        TeamMemberId = m.TeamMemberId,
        Email = m.Email,
        FirstName = m.FirstName,
        LastName = m.LastName,
        DisplayName = m.DisplayName,
        AvatarUrl = m.AvatarUrl,
        Title = m.Title,
        ProfessionalId = m.ProfessionalId,
        Availability = m.Availability,
        FlgStatus = m.FlgStatus
    };

    private static TeamMemberDetailResponse MapToDetailResponse(TeamMember m, IEnumerable<DepartmentMember> memberships) => new()
    {
        TeamMemberId = m.TeamMemberId,
        Email = m.Email,
        FirstName = m.FirstName,
        LastName = m.LastName,
        DisplayName = m.DisplayName,
        AvatarUrl = m.AvatarUrl,
        Title = m.Title,
        ProfessionalId = m.ProfessionalId,
        Availability = m.Availability,
        FlgStatus = m.FlgStatus,
        Skills = !string.IsNullOrEmpty(m.Skills)
            ? JsonSerializer.Deserialize<string[]>(m.Skills, JsonOptions)
            : null,
        MaxConcurrentTasks = m.MaxConcurrentTasks,
        DepartmentMemberships = memberships.Select(dm => new DepartmentMembershipResponse
        {
            DepartmentId = dm.DepartmentId,
            DepartmentName = dm.Department?.DepartmentName ?? string.Empty,
            DepartmentCode = dm.Department?.DepartmentCode ?? string.Empty,
            RoleId = dm.RoleId,
            RoleName = dm.Role?.RoleName ?? string.Empty,
            DateJoined = dm.DateJoined
        }).ToList(),
        DateCreated = m.DateCreated,
        DateUpdated = m.DateUpdated
    };
}
