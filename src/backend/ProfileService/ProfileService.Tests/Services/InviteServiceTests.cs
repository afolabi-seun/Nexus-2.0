using Microsoft.Extensions.Logging;
using Moq;
using ProfileService.Application.DTOs.Invites;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.Invites;
using ProfileService.Domain.Interfaces.Repositories.Organizations;
using ProfileService.Domain.Interfaces.Repositories.Roles;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Domain.Interfaces.Services.Outbox;
using ProfileService.Infrastructure.Configuration;
using ProfileService.Infrastructure.Services.Invites;
using ProfileService.Infrastructure.Services.ServiceClients;
using StackExchange.Redis;

namespace ProfileService.Tests.Services;

public class InviteServiceTests
{
    private readonly Mock<IInviteRepository> _inviteRepo = new();
    private readonly Mock<ITeamMemberRepository> _memberRepo = new();
    private readonly Mock<IDepartmentRepository> _deptRepo = new();
    private readonly Mock<IDepartmentMemberRepository> _deptMemberRepo = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<ISecurityServiceClient> _securityClient = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _redisDb = new();
    private readonly InviteService _service;

    public InviteServiceTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDb.Object);

        var appSettings = new AppSettings
        {
            InviteExpiryHours = 48,
            InviteTokenLength = 128
        };

        _service = new InviteService(
            _inviteRepo.Object,
            _memberRepo.Object,
            _deptRepo.Object,
            _deptMemberRepo.Object,
            _roleRepo.Object,
            _orgRepo.Object,
            _outbox.Object,
            _securityClient.Object,
            _redis.Object,
            appSettings,
            Mock.Of<ILogger<InviteService>>());
    }

    [Fact]
    public async Task CreateAsync_TokenLength_Max128Chars()
    {
        var orgId = Guid.NewGuid();
        var inviterId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _memberRepo.Setup(r => r.GetByEmailAsync(orgId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamMember?)null);
        _inviteRepo.Setup(r => r.AddAsync(It.IsAny<Invite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invite i, CancellationToken _) => i);
        _deptRepo.Setup(r => r.GetByIdAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { DepartmentId = deptId, DepartmentName = "Engineering" });
        _roleRepo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Entities.Role { RoleId = roleId, RoleName = "Member" });

        var request = new CreateInviteRequest
        {
            Email = "new@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            DepartmentId = deptId,
            RoleId = roleId
        };

        await _service.CreateAsync(orgId, inviterId, deptId, RoleNames.OrgAdmin, request);

        _inviteRepo.Verify(r => r.AddAsync(
            It.Is<Invite>(i => i.Token.Length <= 128),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredInvite_ThrowsInviteExpiredOrInvalidException()
    {
        var invite = new Invite
        {
            Token = "expired-token",
            FlgStatus = InviteStatuses.Active,
            ExpiryDate = DateTime.UtcNow.AddHours(-1) // expired
        };

        _inviteRepo.Setup(r => r.GetByTokenAsync("expired-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        await Assert.ThrowsAsync<InviteExpiredOrInvalidException>(
            () => _service.ValidateTokenAsync("expired-token"));
    }

    [Fact]
    public async Task CreateAsync_EmailAlreadyMember_ThrowsInviteEmailAlreadyMemberException()
    {
        var orgId = Guid.NewGuid();
        _memberRepo.Setup(r => r.GetByEmailAsync(orgId, "existing@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeamMember { Email = "existing@example.com" });

        var request = new CreateInviteRequest
        {
            Email = "existing@example.com",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<InviteEmailAlreadyMemberException>(
            () => _service.CreateAsync(orgId, Guid.NewGuid(), Guid.NewGuid(), RoleNames.OrgAdmin, request));
    }
}
