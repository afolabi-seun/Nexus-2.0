using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Application.DTOs.TeamMembers;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.Organizations;
using ProfileService.Domain.Interfaces.Repositories.Roles;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Domain.Interfaces.Services.Organizations;
using ProfileService.Domain.Interfaces.Services.Outbox;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace ProfileService.Infrastructure.Services.Organizations;

public class OrganizationService : IOrganizationService
{
    private static readonly Regex PrefixRegex = new(@"^[A-Z0-9]{2,10}$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IOrganizationRepository _orgRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly ITeamMemberRepository _memberRepo;
    private readonly IDepartmentMemberRepository _deptMemberRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IOutboxService _outbox;
    private readonly ISecurityServiceClient _securityClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly ProfileDbContext _dbContext;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IOrganizationRepository orgRepo,
        IDepartmentRepository deptRepo,
        ITeamMemberRepository memberRepo,
        IDepartmentMemberRepository deptMemberRepo,
        IRoleRepository roleRepo,
        IOutboxService outbox,
        ISecurityServiceClient securityClient,
        IConnectionMultiplexer redis,
        ProfileDbContext dbContext,
        ILogger<OrganizationService> logger)
    {
        _orgRepo = orgRepo;
        _deptRepo = deptRepo;
        _memberRepo = memberRepo;
        _deptMemberRepo = deptMemberRepo;
        _roleRepo = roleRepo;
        _outbox = outbox;
        _securityClient = securityClient;
        _redis = redis;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<object> CreateAsync(object request, CancellationToken ct = default)
    {
        var req = (CreateOrganizationRequest)request;

        // Validate name uniqueness
        var existingByName = await _orgRepo.GetByNameAsync(req.OrganizationName, ct);
        if (existingByName is not null)
            throw new OrganizationNameDuplicateException();

        // Validate prefix format
        if (!PrefixRegex.IsMatch(req.StoryIdPrefix))
            throw new StoryPrefixInvalidFormatException();

        // Validate prefix uniqueness
        var existingByPrefix = await _orgRepo.GetByStoryIdPrefixAsync(req.StoryIdPrefix, ct);
        if (existingByPrefix is not null)
            throw new StoryPrefixDuplicateException();

        var org = new Organization
        {
            OrganizationName = req.OrganizationName,
            StoryIdPrefix = req.StoryIdPrefix,
            Description = req.Description,
            Website = req.Website,
            LogoUrl = req.LogoUrl,
            TimeZone = req.TimeZone,
            DefaultSprintDurationWeeks = req.DefaultSprintDurationWeeks,
            FlgStatus = EntityStatuses.Active,
            SettingsJson = JsonSerializer.Serialize(new OrganizationSettings(), JsonOptions)
        };

        await _orgRepo.AddAsync(org, ct);

        // Seed 5 default departments
        await SeedData.SeedDefaultDepartmentsAsync(_dbContext, org.OrganizationId);

        // Publish audit event
        await _outbox.PublishAsync(new OutboxMessage
        {
            MessageType = "AuditEvent",
            OrganizationId = org.OrganizationId,
            Action = "OrganizationCreated",
            EntityType = "Organization",
            EntityId = org.OrganizationId.ToString()
        }, ct);

        return MapToResponse(org);
    }

    public async Task<object> GetByIdAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await _orgRepo.GetByIdAsync(organizationId, ct)
            ?? throw new NotFoundException($"Organization {organizationId} not found");
        return MapToResponse(org);
    }

    public async Task<object> UpdateAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (UpdateOrganizationRequest)request;
        var org = await _orgRepo.GetByIdAsync(organizationId, ct)
            ?? throw new NotFoundException($"Organization {organizationId} not found");

        // Validate name uniqueness if changed
        if (req.OrganizationName is not null && req.OrganizationName != org.OrganizationName)
        {
            var existing = await _orgRepo.GetByNameAsync(req.OrganizationName, ct);
            if (existing is not null)
                throw new OrganizationNameDuplicateException();
            org.OrganizationName = req.OrganizationName;
        }

        if (req.Description is not null) org.Description = req.Description;
        if (req.Website is not null) org.Website = req.Website;
        if (req.LogoUrl is not null) org.LogoUrl = req.LogoUrl;
        if (req.TimeZone is not null) org.TimeZone = req.TimeZone;
        if (req.DefaultSprintDurationWeeks.HasValue) org.DefaultSprintDurationWeeks = req.DefaultSprintDurationWeeks.Value;

        org.DateUpdated = DateTime.UtcNow;
        await _orgRepo.UpdateAsync(org, ct);

        return MapToResponse(org);
    }

    public async Task UpdateStatusAsync(Guid organizationId, string newStatus, CancellationToken ct = default)
    {
        var org = await _orgRepo.GetByIdAsync(organizationId, ct)
            ?? throw new NotFoundException($"Organization {organizationId} not found");

        // Enforce A → S → D lifecycle
        var valid = (org.FlgStatus, newStatus) switch
        {
            (EntityStatuses.Active, EntityStatuses.Suspended) => true,
            (EntityStatuses.Suspended, EntityStatuses.Deactivated) => true,
            (EntityStatuses.Suspended, EntityStatuses.Active) => true,
            _ => false
        };

        if (!valid)
            throw new DomainException(
                ErrorCodes.ConflictValue, ErrorCodes.Conflict,
                $"Cannot transition from '{org.FlgStatus}' to '{newStatus}'",
                System.Net.HttpStatusCode.Conflict);

        org.FlgStatus = newStatus;
        org.DateUpdated = DateTime.UtcNow;
        await _orgRepo.UpdateAsync(org, ct);
    }

    public async Task<object> UpdateSettingsAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (OrganizationSettingsRequest)request;
        var org = await _orgRepo.GetByIdAsync(organizationId, ct)
            ?? throw new NotFoundException($"Organization {organizationId} not found");

        var settings = !string.IsNullOrEmpty(org.SettingsJson)
            ? JsonSerializer.Deserialize<OrganizationSettings>(org.SettingsJson, JsonOptions) ?? new OrganizationSettings()
            : new OrganizationSettings();

        // Validate StoryIdPrefix
        if (req.StoryIdPrefix is not null)
        {
            if (!PrefixRegex.IsMatch(req.StoryIdPrefix))
                throw new StoryPrefixInvalidFormatException();

            // Immutability: once set, cannot change
            if (!string.IsNullOrEmpty(org.StoryIdPrefix) && org.StoryIdPrefix != req.StoryIdPrefix)
                throw new StoryPrefixImmutableException();

            // Uniqueness
            var existingByPrefix = await _orgRepo.GetByStoryIdPrefixAsync(req.StoryIdPrefix, ct);
            if (existingByPrefix is not null && existingByPrefix.OrganizationId != organizationId)
                throw new StoryPrefixDuplicateException();
        }

        // Update top-level org fields
        if (req.TimeZone is not null) org.TimeZone = req.TimeZone;
        if (req.DefaultSprintDurationWeeks.HasValue) org.DefaultSprintDurationWeeks = req.DefaultSprintDurationWeeks.Value;

        // Update settings JSON fields
        if (req.WorkingDays is not null) settings.WorkingDays = req.WorkingDays;
        if (req.WorkingHoursStart is not null) settings.WorkingHoursStart = req.WorkingHoursStart;
        if (req.WorkingHoursEnd is not null) settings.WorkingHoursEnd = req.WorkingHoursEnd;
        if (req.PrimaryColor is not null) settings.PrimaryColor = req.PrimaryColor;
        if (req.StoryPointScale is not null) settings.StoryPointScale = req.StoryPointScale;
        if (req.RequiredFieldsByStoryType is not null) settings.RequiredFieldsByStoryType = req.RequiredFieldsByStoryType;
        if (req.AutoAssignmentEnabled.HasValue) settings.AutoAssignmentEnabled = req.AutoAssignmentEnabled.Value;
        if (req.AutoAssignmentStrategy is not null) settings.AutoAssignmentStrategy = req.AutoAssignmentStrategy;
        if (req.DefaultBoardView is not null) settings.DefaultBoardView = req.DefaultBoardView;
        if (req.WipLimitsEnabled.HasValue) settings.WipLimitsEnabled = req.WipLimitsEnabled.Value;
        if (req.DefaultWipLimit.HasValue) settings.DefaultWipLimit = req.DefaultWipLimit.Value;
        if (req.DefaultNotificationChannels is not null) settings.DefaultNotificationChannels = req.DefaultNotificationChannels;
        if (req.DigestFrequency is not null) settings.DigestFrequency = req.DigestFrequency;
        if (req.AuditRetentionDays.HasValue) settings.AuditRetentionDays = req.AuditRetentionDays.Value;

        org.SettingsJson = JsonSerializer.Serialize(settings, JsonOptions);
        org.DateUpdated = DateTime.UtcNow;
        await _orgRepo.UpdateAsync(org, ct);

        // Invalidate cache
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"org_settings:{organizationId}");

        return MapSettingsToResponse(settings);
    }

    public async Task<object> ListAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _orgRepo.ListAllAsync(page, pageSize, ct);
        return new PaginatedResponse<OrganizationResponse>
        {
            Data = items.Select(MapToResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<object> ProvisionAdminAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (ProvisionAdminRequest)request;
        var org = await _orgRepo.GetByIdAsync(organizationId, ct)
            ?? throw new NotFoundException($"Organization {organizationId} not found");

        // Check email not already registered
        var existingMember = await _memberRepo.GetByEmailAsync(organizationId, req.Email, ct);
        if (existingMember is not null)
            throw new EmailAlreadyRegisteredException();

        // Get Engineering department
        var engDept = await _deptRepo.GetByCodeAsync(organizationId, DepartmentTypes.EngineeringCode, ct)
            ?? throw new DepartmentNotFoundException($"Engineering department not found for org {organizationId}");

        // Get OrgAdmin role
        var orgAdminRole = await _roleRepo.GetByNameAsync(RoleNames.OrgAdmin, ct)
            ?? throw new NotFoundException("OrgAdmin role not found");

        // Generate professional ID
        var seqNum = await _memberRepo.GetNextSequentialNumberAsync(organizationId, engDept.DepartmentCode, ct);
        var professionalId = $"NXS-{engDept.DepartmentCode}-{seqNum:D3}";

        // Create TeamMember
        var member = new TeamMember
        {
            OrganizationId = organizationId,
            PrimaryDepartmentId = engDept.DepartmentId,
            Email = req.Email,
            Password = string.Empty, // Will be set by SecurityService
            FirstName = req.FirstName,
            LastName = req.LastName,
            DisplayName = $"{req.FirstName} {req.LastName}",
            ProfessionalId = professionalId,
            FlgStatus = EntityStatuses.Active,
            IsFirstTimeUser = true
        };
        await _memberRepo.AddAsync(member, ct);

        // Create DepartmentMember
        var deptMember = new DepartmentMember
        {
            TeamMemberId = member.TeamMemberId,
            DepartmentId = engDept.DepartmentId,
            OrganizationId = organizationId,
            RoleId = orgAdminRole.RoleId
        };
        await _deptMemberRepo.AddAsync(deptMember, ct);

        // Call SecurityService to generate credentials
        await _securityClient.GenerateCredentialsAsync(member.TeamMemberId, req.Email, ct);

        // Publish audit event
        await _outbox.PublishAsync(new OutboxMessage
        {
            MessageType = "AuditEvent",
            OrganizationId = organizationId,
            UserId = member.TeamMemberId,
            Action = "MemberCreated",
            EntityType = "TeamMember",
            EntityId = member.TeamMemberId.ToString()
        }, ct);

        return MapToDetailResponse(member, [deptMember], engDept, orgAdminRole);
    }

    private static OrganizationResponse MapToResponse(Organization org)
    {
        OrganizationSettingsResponse? settings = null;
        if (!string.IsNullOrEmpty(org.SettingsJson))
        {
            var parsed = JsonSerializer.Deserialize<OrganizationSettings>(org.SettingsJson, JsonOptions);
            if (parsed is not null)
                settings = MapSettingsToResponse(parsed);
        }

        return new OrganizationResponse
        {
            OrganizationId = org.OrganizationId,
            OrganizationName = org.OrganizationName,
            StoryIdPrefix = org.StoryIdPrefix,
            Description = org.Description,
            Website = org.Website,
            LogoUrl = org.LogoUrl,
            TimeZone = org.TimeZone,
            DefaultSprintDurationWeeks = org.DefaultSprintDurationWeeks,
            Settings = settings,
            FlgStatus = org.FlgStatus,
            DateCreated = org.DateCreated,
            DateUpdated = org.DateUpdated
        };
    }

    private static OrganizationSettingsResponse MapSettingsToResponse(OrganizationSettings s) => new()
    {
        StoryPointScale = s.StoryPointScale,
        RequiredFieldsByStoryType = s.RequiredFieldsByStoryType,
        AutoAssignmentEnabled = s.AutoAssignmentEnabled,
        AutoAssignmentStrategy = s.AutoAssignmentStrategy,
        WorkingDays = s.WorkingDays,
        WorkingHoursStart = s.WorkingHoursStart,
        WorkingHoursEnd = s.WorkingHoursEnd,
        PrimaryColor = s.PrimaryColor,
        DefaultBoardView = s.DefaultBoardView,
        WipLimitsEnabled = s.WipLimitsEnabled,
        DefaultWipLimit = s.DefaultWipLimit,
        DefaultNotificationChannels = s.DefaultNotificationChannels,
        DigestFrequency = s.DigestFrequency,
        AuditRetentionDays = s.AuditRetentionDays
    };

    private static TeamMemberDetailResponse MapToDetailResponse(
        TeamMember member, IEnumerable<DepartmentMember> memberships, Department dept, Domain.Entities.Role role) => new()
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
        FlgStatus = member.FlgStatus,
        Skills = !string.IsNullOrEmpty(member.Skills)
            ? JsonSerializer.Deserialize<string[]>(member.Skills, JsonOptions)
            : null,
        MaxConcurrentTasks = member.MaxConcurrentTasks,
        DepartmentMemberships = memberships.Select(dm => new DepartmentMembershipResponse
        {
            DepartmentId = dm.DepartmentId,
            DepartmentName = dept.DepartmentName,
            DepartmentCode = dept.DepartmentCode,
            RoleId = dm.RoleId,
            RoleName = role.RoleName,
            DateJoined = dm.DateJoined
        }).ToList(),
        DateCreated = member.DateCreated,
        DateUpdated = member.DateUpdated
    };
}
