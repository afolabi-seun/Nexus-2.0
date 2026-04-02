using Microsoft.Extensions.Logging;
using Moq;
using ProfileService.Application.DTOs.TeamMembers;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.Roles;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Infrastructure.Services.TeamMembers;
using ProfileService.Tests.Helpers;
using StackExchange.Redis;

namespace ProfileService.Tests.Services;

public class TeamMemberServiceTests
{
    private readonly Mock<ITeamMemberRepository> _memberRepo = new();
    private readonly Mock<IDepartmentMemberRepository> _deptMemberRepo = new();
    private readonly Mock<IDepartmentRepository> _deptRepo = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _redisDb = new();
    private readonly TeamMemberService _service;

    public TeamMemberServiceTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDb.Object);

        _service = new TeamMemberService(
            _memberRepo.Object,
            _deptMemberRepo.Object,
            _deptRepo.Object,
            _roleRepo.Object,
            _redis.Object,
            TestDbContextFactory.Create(),
            Mock.Of<ILogger<TeamMemberService>>());
    }

    [Fact]
    public async Task UpdateStatusAsync_LastOrgAdmin_ThrowsLastOrgAdminCannotDeactivateException()
    {
        var orgId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var orgAdminRole = new Domain.Entities.Role { RoleId = roleId, RoleName = RoleNames.OrgAdmin, PermissionLevel = 100 };

        var member = new TeamMember
        {
            TeamMemberId = memberId,
            OrganizationId = orgId,
            FlgStatus = EntityStatuses.Active
        };

        _memberRepo.Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _deptMemberRepo.Setup(r => r.GetByMemberIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DepartmentMember>
            {
                new() { TeamMemberId = memberId, RoleId = roleId, Role = orgAdminRole }
            });
        _roleRepo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orgAdminRole);
        _memberRepo.Setup(r => r.CountOrgAdminsAsync(orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await Assert.ThrowsAsync<LastOrgAdminCannotDeactivateException>(
            () => _service.UpdateStatusAsync(memberId, EntityStatuses.Deactivated));
    }

    [Fact]
    public void ProfessionalId_Format_MatchesExpected()
    {
        // Professional ID format: NXS-{DeptCode}-{NNN}
        var deptCode = "ENG";
        var seqNum = 42;
        var professionalId = $"NXS-{deptCode}-{seqNum:D3}";

        Assert.Equal("NXS-ENG-042", professionalId);
        Assert.Matches(@"^NXS-[A-Z]+-\d{3}$", professionalId);
    }

    [Fact]
    public async Task AddToDepartmentAsync_DuplicateMembership_ThrowsMemberAlreadyInDepartmentException()
    {
        var memberId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var member = new TeamMember
        {
            TeamMemberId = memberId,
            OrganizationId = Guid.NewGuid()
        };

        _memberRepo.Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _deptMemberRepo.Setup(r => r.GetAsync(memberId, deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DepartmentMember { TeamMemberId = memberId, DepartmentId = deptId });

        var request = new AddDepartmentRequest { DepartmentId = deptId, RoleId = roleId };

        await Assert.ThrowsAsync<MemberAlreadyInDepartmentException>(
            () => _service.AddToDepartmentAsync(memberId, request));
    }

    [Fact]
    public async Task AddToDepartmentAsync_MultiDepartmentWithDifferentRoles_Succeeds()
    {
        var memberId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var member = new TeamMember
        {
            TeamMemberId = memberId,
            OrganizationId = orgId
        };

        _memberRepo.Setup(r => r.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _deptMemberRepo.Setup(r => r.GetAsync(memberId, deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentMember?)null);
        _deptRepo.Setup(r => r.GetByIdAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { DepartmentId = deptId, OrganizationId = orgId });
        _deptMemberRepo.Setup(r => r.AddAsync(It.IsAny<DepartmentMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentMember dm, CancellationToken _) => dm);

        var request = new AddDepartmentRequest { DepartmentId = deptId, RoleId = roleId };

        await _service.AddToDepartmentAsync(memberId, request);

        _deptMemberRepo.Verify(r => r.AddAsync(
            It.Is<DepartmentMember>(dm => dm.DepartmentId == deptId && dm.RoleId == roleId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
