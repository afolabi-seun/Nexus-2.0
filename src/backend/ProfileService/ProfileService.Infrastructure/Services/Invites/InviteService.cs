using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Invites;
using ProfileService.Application.DTOs.TeamMembers;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories;
using ProfileService.Domain.Interfaces.Services;
using ProfileService.Infrastructure.Configuration;
using ProfileService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace ProfileService.Infrastructure.Services.Invites;

public class InviteService : IInviteService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IInviteRepository _inviteRepo;
    private readonly ITeamMemberRepository _memberRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IDepartmentMemberRepository _deptMemberRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IOrganizationRepository _orgRepo;
    private readonly IOutboxService _outbox;
    private readonly ISecurityServiceClient _securityClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly AppSettings _appSettings;
    private readonly ILogger<InviteService> _logger;

    public InviteService(
        IInviteRepository inviteRepo,
        ITeamMemberRepository memberRepo,
        IDepartmentRepository deptRepo,
        IDepartmentMemberRepository deptMemberRepo,
        IRoleRepository roleRepo,
        IOrganizationRepository orgRepo,
        IOutboxService outbox,
        ISecurityServiceClient securityClient,
        IConnectionMultiplexer redis,
        AppSettings appSettings,
        ILogger<InviteService> logger)
    {
        _inviteRepo = inviteRepo;
        _memberRepo = memberRepo;
        _deptRepo = deptRepo;
        _deptMemberRepo = deptMemberRepo;
        _roleRepo = roleRepo;
        _orgRepo = orgRepo;
        _outbox = outbox;
        _securityClient = securityClient;
        _redis = redis;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<object> CreateAsync(Guid organizationId, Guid invitedByMemberId,
        Guid inviterDepartmentId, string inviterRole, object request, CancellationToken ct = default)
    {
        var req = (CreateInviteRequest)request;

        // Validate email not already a member
        var existingMember = await _memberRepo.GetByEmailAsync(organizationId, req.Email, ct);
        if (existingMember is not null)
            throw new InviteEmailAlreadyMemberException();

        // Scope DeptLead to own department
        if (inviterRole == RoleNames.DeptLead && req.DepartmentId != inviterDepartmentId)
            throw new DomainException(
                ErrorCodes.OrganizationMismatchValue, ErrorCodes.OrganizationMismatch,
                "DeptLead can only invite to their own department",
                System.Net.HttpStatusCode.Forbidden);

        // Generate cryptographic token
        var tokenBytes = RandomNumberGenerator.GetBytes(_appSettings.InviteTokenLength / 2);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        if (token.Length > 128) token = token[..128];

        var invite = new Invite
        {
            OrganizationId = organizationId,
            DepartmentId = req.DepartmentId,
            RoleId = req.RoleId,
            InvitedByMemberId = invitedByMemberId,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            Token = token,
            ExpiryDate = DateTime.UtcNow.AddHours(_appSettings.InviteExpiryHours),
            FlgStatus = InviteStatuses.Active
        };

        await _inviteRepo.AddAsync(invite, ct);

        // Publish notification
        await _outbox.PublishAsync(new OutboxMessage
        {
            MessageType = "NotificationRequest",
            OrganizationId = organizationId,
            Action = "MemberInvited",
            EntityType = "Invite",
            EntityId = invite.InviteId.ToString(),
            NewValue = JsonSerializer.Serialize(new { invite.Email, invite.FirstName, invite.LastName }, JsonOptions)
        }, ct);

        var dept = await _deptRepo.GetByIdAsync(req.DepartmentId, ct);
        var role = await _roleRepo.GetByIdAsync(req.RoleId, ct);

        return new InviteResponse
        {
            InviteId = invite.InviteId,
            Email = invite.Email,
            FirstName = invite.FirstName,
            LastName = invite.LastName,
            DepartmentName = dept?.DepartmentName ?? string.Empty,
            RoleName = role?.RoleName ?? string.Empty,
            FlgStatus = invite.FlgStatus,
            ExpiryDate = invite.ExpiryDate,
            DateCreated = invite.DateCreated
        };
    }

    public async Task<object> ListAsync(Guid organizationId, Guid? departmentId, string role,
        int page, int pageSize, CancellationToken ct = default)
    {
        // OrgAdmin sees all, DeptLead sees own department
        Guid? filterDeptId = role == RoleNames.DeptLead ? departmentId : null;

        var (items, totalCount) = await _inviteRepo.ListPendingAsync(organizationId, filterDeptId, page, pageSize, ct);
        var responses = new List<InviteResponse>();

        foreach (var invite in items)
        {
            var dept = invite.Department ?? await _deptRepo.GetByIdAsync(invite.DepartmentId, ct);
            var inviteRole = invite.Role ?? await _roleRepo.GetByIdAsync(invite.RoleId, ct);

            responses.Add(new InviteResponse
            {
                InviteId = invite.InviteId,
                Email = invite.Email,
                FirstName = invite.FirstName,
                LastName = invite.LastName,
                DepartmentName = dept?.DepartmentName ?? string.Empty,
                RoleName = inviteRole?.RoleName ?? string.Empty,
                FlgStatus = invite.FlgStatus,
                ExpiryDate = invite.ExpiryDate,
                DateCreated = invite.DateCreated
            });
        }

        return new PaginatedResponse<InviteResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<object> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var invite = await _inviteRepo.GetByTokenAsync(token, ct);
        if (invite is null || invite.FlgStatus != InviteStatuses.Active || invite.ExpiryDate < DateTime.UtcNow)
            throw new InviteExpiredOrInvalidException();

        var org = await _orgRepo.GetByIdAsync(invite.OrganizationId, ct);
        var dept = invite.Department ?? await _deptRepo.GetByIdAsync(invite.DepartmentId, ct);
        var role = invite.Role ?? await _roleRepo.GetByIdAsync(invite.RoleId, ct);

        return new InviteValidationResponse
        {
            OrganizationName = org?.OrganizationName ?? string.Empty,
            DepartmentName = dept?.DepartmentName ?? string.Empty,
            RoleName = role?.RoleName ?? string.Empty
        };
    }

    public async Task AcceptAsync(string token, object request, CancellationToken ct = default)
    {
        var invite = await _inviteRepo.GetByTokenAsync(token, ct);
        if (invite is null || invite.FlgStatus != InviteStatuses.Active || invite.ExpiryDate < DateTime.UtcNow)
            throw new InviteExpiredOrInvalidException();

        // Check email not already registered
        var existingMember = await _memberRepo.GetByEmailAsync(invite.OrganizationId, invite.Email, ct);
        if (existingMember is not null)
            throw new InviteEmailAlreadyMemberException();

        var dept = await _deptRepo.GetByIdAsync(invite.DepartmentId, ct)
            ?? throw new DepartmentNotFoundException($"Department {invite.DepartmentId} not found");

        // Generate professional ID
        var seqNum = await _memberRepo.GetNextSequentialNumberAsync(invite.OrganizationId, dept.DepartmentCode, ct);
        var professionalId = $"NXS-{dept.DepartmentCode}-{seqNum:D3}";

        // Create TeamMember
        var member = new TeamMember
        {
            OrganizationId = invite.OrganizationId,
            PrimaryDepartmentId = invite.DepartmentId,
            Email = invite.Email,
            Password = string.Empty,
            FirstName = invite.FirstName,
            LastName = invite.LastName,
            DisplayName = $"{invite.FirstName} {invite.LastName}",
            ProfessionalId = professionalId,
            FlgStatus = EntityStatuses.Active,
            IsFirstTimeUser = true
        };
        await _memberRepo.AddAsync(member, ct);

        // Create DepartmentMember
        var deptMember = new DepartmentMember
        {
            TeamMemberId = member.TeamMemberId,
            DepartmentId = invite.DepartmentId,
            OrganizationId = invite.OrganizationId,
            RoleId = invite.RoleId
        };
        await _deptMemberRepo.AddAsync(deptMember, ct);

        // Call SecurityService to generate credentials
        await _securityClient.GenerateCredentialsAsync(member.TeamMemberId, invite.Email, ct);

        // Update invite status to Used
        invite.FlgStatus = InviteStatuses.Used;
        await _inviteRepo.UpdateAsync(invite, ct);

        // Invalidate caches
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"dept_list:{invite.OrganizationId}");
        await db.KeyDeleteAsync($"member_profile:{member.TeamMemberId}");

        // Publish audit event
        await _outbox.PublishAsync(new OutboxMessage
        {
            MessageType = "AuditEvent",
            OrganizationId = invite.OrganizationId,
            UserId = member.TeamMemberId,
            Action = "InviteAccepted",
            EntityType = "Invite",
            EntityId = invite.InviteId.ToString()
        }, ct);
    }

    public async Task CancelAsync(Guid inviteId, CancellationToken ct = default)
    {
        var invite = await _inviteRepo.GetByIdAsync(inviteId, ct)
            ?? throw new NotFoundException($"Invite {inviteId} not found");

        invite.FlgStatus = InviteStatuses.Expired;
        await _inviteRepo.UpdateAsync(invite, ct);
    }
}
