using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProfileService.Application.DTOs.Organizations;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Exceptions;
using ProfileService.Domain.Helpers;
using ProfileService.Domain.Interfaces.Repositories.DepartmentMembers;
using ProfileService.Domain.Interfaces.Repositories.Departments;
using ProfileService.Domain.Interfaces.Repositories.Organizations;
using ProfileService.Domain.Interfaces.Repositories.Roles;
using ProfileService.Domain.Interfaces.Repositories.TeamMembers;
using ProfileService.Domain.Interfaces.Services.Outbox;
using ProfileService.Infrastructure.Data;
using ProfileService.Infrastructure.Services.Organizations;
using ProfileService.Infrastructure.Services.ServiceClients;
using ProfileService.Tests.Helpers;
using StackExchange.Redis;

namespace ProfileService.Tests.Services;

public class OrganizationServiceTests
{
    private readonly ProfileDbContext _dbContext;
    private readonly Mock<IOrganizationRepository> _orgRepo = new();
    private readonly Mock<IDepartmentRepository> _deptRepo = new();
    private readonly Mock<ITeamMemberRepository> _memberRepo = new();
    private readonly Mock<IDepartmentMemberRepository> _deptMemberRepo = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<ISecurityServiceClient> _securityClient = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _redisDb = new();
    private readonly OrganizationService _service;

    public OrganizationServiceTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDb.Object);

        _service = new OrganizationService(
            _orgRepo.Object,
            _deptRepo.Object,
            _memberRepo.Object,
            _deptMemberRepo.Object,
            _roleRepo.Object,
            _outbox.Object,
            _securityClient.Object,
            _redis.Object,
            _dbContext,
            Mock.Of<ILogger<OrganizationService>>());
    }

    [Fact]
    public async Task CreateAsync_SeedsDefaultDepartments()
    {
        // Arrange
        _orgRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);
        _orgRepo.Setup(r => r.GetByStoryIdPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);
        _orgRepo.Setup(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization o, CancellationToken _) => o);

        var request = new CreateOrganizationRequest
        {
            OrganizationName = "TestOrg",
            StoryIdPrefix = "TEST",
            TimeZone = "UTC",
            DefaultSprintDurationWeeks = 2
        };

        // Act
        await _service.CreateAsync(request);

        // Assert — 5 default departments seeded in the in-memory DB
        // Use IgnoreQueryFilters to avoid the global filter issue with null _organizationId
        var departments = _dbContext.Departments.IgnoreQueryFilters().Where(d => d.IsDefault).ToList();
        Assert.Equal(5, departments.Count);
        Assert.Contains(departments, d => d.DepartmentCode == "ENG");
        Assert.Contains(departments, d => d.DepartmentCode == "QA");
        Assert.Contains(departments, d => d.DepartmentCode == "DEVOPS");
        Assert.Contains(departments, d => d.DepartmentCode == "PROD");
        Assert.Contains(departments, d => d.DepartmentCode == "DESIGN");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsOrganizationNameDuplicateException()
    {
        _orgRepo.Setup(r => r.GetByNameAsync("Existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { OrganizationName = "Existing" });

        var request = new CreateOrganizationRequest
        {
            OrganizationName = "Existing",
            StoryIdPrefix = "EX",
            TimeZone = "UTC"
        };

        await Assert.ThrowsAsync<OrganizationNameDuplicateException>(
            () => _service.CreateAsync(request));
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("NEXUS2024")]
    [InlineData("X1")]
    public async Task CreateAsync_ValidStoryIdPrefix_Succeeds(string prefix)
    {
        _orgRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);
        _orgRepo.Setup(r => r.GetByStoryIdPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);
        _orgRepo.Setup(r => r.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization o, CancellationToken _) => o);

        var request = new CreateOrganizationRequest
        {
            OrganizationName = $"Org-{prefix}",
            StoryIdPrefix = prefix,
            TimeZone = "UTC"
        };

        var result = await _service.CreateAsync(request);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("ab")]    // lowercase
    [InlineData("A")]     // too short
    [InlineData("AB!@#")] // special chars
    public async Task CreateAsync_InvalidStoryIdPrefix_Throws(string prefix)
    {
        _orgRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        var request = new CreateOrganizationRequest
        {
            OrganizationName = "TestOrg",
            StoryIdPrefix = prefix,
            TimeZone = "UTC"
        };

        await Assert.ThrowsAsync<StoryPrefixInvalidFormatException>(
            () => _service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateStatusAsync_ActiveToSuspended_Succeeds()
    {
        var org = new Organization { FlgStatus = EntityStatuses.Active };
        _orgRepo.Setup(r => r.GetByIdAsync(org.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        await _service.UpdateStatusAsync(org.OrganizationId, EntityStatuses.Suspended);

        _orgRepo.Verify(r => r.UpdateAsync(It.Is<Organization>(o => o.FlgStatus == EntityStatuses.Suspended), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_SuspendedToDeactivated_Succeeds()
    {
        var org = new Organization { FlgStatus = EntityStatuses.Suspended };
        _orgRepo.Setup(r => r.GetByIdAsync(org.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        await _service.UpdateStatusAsync(org.OrganizationId, EntityStatuses.Deactivated);

        _orgRepo.Verify(r => r.UpdateAsync(It.Is<Organization>(o => o.FlgStatus == EntityStatuses.Deactivated), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ActiveToDeactivated_ThrowsConflict()
    {
        var org = new Organization { FlgStatus = EntityStatuses.Active };
        _orgRepo.Setup(r => r.GetByIdAsync(org.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(org);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateStatusAsync(org.OrganizationId, EntityStatuses.Deactivated));
        Assert.Equal(ErrorCodes.Conflict, ex.ErrorCode);
    }
}
