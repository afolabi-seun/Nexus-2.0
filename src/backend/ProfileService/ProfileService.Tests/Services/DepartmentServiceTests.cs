using Microsoft.Extensions.Logging;
using Moq;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Services.Departments;
using ProfileService.Tests.Helpers;
using StackExchange.Redis;

namespace ProfileService.Tests.Services;

public class DepartmentServiceTests
{
    private readonly Mock<IDepartmentRepository> _deptRepo = new();
    private readonly Mock<IDepartmentMemberRepository> _deptMemberRepo = new();
    private readonly Mock<ITeamMemberRepository> _memberRepo = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _redisDb = new();
    private readonly DepartmentService _service;

    public DepartmentServiceTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDb.Object);

        _service = new DepartmentService(
            _deptRepo.Object,
            _deptMemberRepo.Object,
            _memberRepo.Object,
            _redis.Object,
            TestDbContextFactory.Create(),
            Mock.Of<ILogger<DepartmentService>>());
    }

    [Fact]
    public async Task UpdateStatusAsync_DefaultDepartment_ThrowsDefaultDepartmentCannotDeleteException()
    {
        var dept = new Department
        {
            DepartmentId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            DepartmentName = "Engineering",
            DepartmentCode = "ENG",
            IsDefault = true,
            FlgStatus = EntityStatuses.Active
        };

        _deptRepo.Setup(r => r.GetByIdAsync(dept.DepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept);

        await Assert.ThrowsAsync<DefaultDepartmentCannotDeleteException>(
            () => _service.UpdateStatusAsync(dept.DepartmentId, EntityStatuses.Deactivated));
    }

    [Fact]
    public async Task UpdateStatusAsync_DepartmentWithActiveMembers_ThrowsDepartmentHasActiveMembersException()
    {
        var dept = new Department
        {
            DepartmentId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            DepartmentName = "Custom",
            DepartmentCode = "CUST",
            IsDefault = false,
            FlgStatus = EntityStatuses.Active
        };

        _deptRepo.Setup(r => r.GetByIdAsync(dept.DepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept);
        _deptRepo.Setup(r => r.GetActiveMemberCountAsync(dept.DepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        await Assert.ThrowsAsync<DepartmentHasActiveMembersException>(
            () => _service.UpdateStatusAsync(dept.DepartmentId, EntityStatuses.Deactivated));
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsDepartmentNameDuplicateException()
    {
        var orgId = Guid.NewGuid();
        _deptRepo.Setup(r => r.GetByNameAsync(orgId, "Engineering", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { DepartmentName = "Engineering" });

        var request = new Application.DTOs.Departments.CreateDepartmentRequest
        {
            DepartmentName = "Engineering",
            DepartmentCode = "ENG2"
        };

        await Assert.ThrowsAsync<DepartmentNameDuplicateException>(
            () => _service.CreateAsync(orgId, request));
    }
}
