using Moq;
using StackExchange.Redis;
using UtilityService.Application.DTOs.ReferenceData;
using UtilityService.Domain.Entities;
using UtilityService.Domain.Exceptions;
using UtilityService.Domain.Interfaces.Repositories;
using UtilityService.Infrastructure.Services.ReferenceData;

namespace UtilityService.Tests.Services;

public class ReferenceDataServiceTests
{
    private readonly Mock<IDepartmentTypeRepository> _deptRepoMock = new();
    private readonly Mock<IPriorityLevelRepository> _priorityRepoMock = new();
    private readonly Mock<ITaskTypeRefRepository> _taskTypeRepoMock = new();
    private readonly Mock<IWorkflowStateRepository> _workflowRepoMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly ReferenceDataService _sut;

    public ReferenceDataServiceTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        _dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        _dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _dbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _sut = new ReferenceDataService(
            _deptRepoMock.Object, _priorityRepoMock.Object,
            _taskTypeRepoMock.Object, _workflowRepoMock.Object,
            _redisMock.Object);
    }

    [Fact]
    public async Task CreateDepartmentTypeAsync_NewType_Succeeds()
    {
        _deptRepoMock.Setup(r => r.ExistsAsync("NewDept", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _deptRepoMock.Setup(r => r.GetByCodeAsync("ND", It.IsAny<CancellationToken>())).ReturnsAsync((DepartmentType?)null);
        _deptRepoMock.Setup(r => r.AddAsync(It.IsAny<DepartmentType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentType e, CancellationToken _) => e);

        var result = await _sut.CreateDepartmentTypeAsync(new CreateDepartmentTypeRequest { TypeName = "NewDept", TypeCode = "ND" });

        var response = Assert.IsType<DepartmentTypeResponse>(result);
        Assert.Equal("NewDept", response.TypeName);
        Assert.Equal("ND", response.TypeCode);
    }

    [Fact]
    public async Task CreateDepartmentTypeAsync_DuplicateName_ThrowsReferenceDataDuplicateException()
    {
        _deptRepoMock.Setup(r => r.ExistsAsync("Engineering", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ReferenceDataDuplicateException>(
            () => _sut.CreateDepartmentTypeAsync(new CreateDepartmentTypeRequest { TypeName = "Engineering", TypeCode = "ENG2" }));
    }

    [Fact]
    public async Task CreateDepartmentTypeAsync_DuplicateCode_ThrowsReferenceDataDuplicateException()
    {
        _deptRepoMock.Setup(r => r.ExistsAsync("NewName", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _deptRepoMock.Setup(r => r.GetByCodeAsync("ENG", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DepartmentType { TypeName = "Engineering", TypeCode = "ENG" });

        await Assert.ThrowsAsync<ReferenceDataDuplicateException>(
            () => _sut.CreateDepartmentTypeAsync(new CreateDepartmentTypeRequest { TypeName = "NewName", TypeCode = "ENG" }));
    }

    [Fact]
    public async Task CreatePriorityLevelAsync_DuplicateName_ThrowsReferenceDataDuplicateException()
    {
        _priorityRepoMock.Setup(r => r.ExistsAsync("Critical", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ReferenceDataDuplicateException>(
            () => _sut.CreatePriorityLevelAsync(new CreatePriorityLevelRequest { Name = "Critical", SortOrder = 1, Color = "#FF0000" }));
    }

    [Fact]
    public async Task GetDepartmentTypesAsync_ReturnsSeedValues()
    {
        var seedDepts = new List<DepartmentType>
        {
            new() { TypeName = "Engineering", TypeCode = "ENG" },
            new() { TypeName = "QA", TypeCode = "QA" },
            new() { TypeName = "DevOps", TypeCode = "DEVOPS" },
            new() { TypeName = "Product", TypeCode = "PROD" },
            new() { TypeName = "Design", TypeCode = "DESIGN" }
        };
        _deptRepoMock.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(seedDepts);

        var result = (await _sut.GetDepartmentTypesAsync()).Cast<DepartmentTypeResponse>().ToList();

        Assert.Equal(5, result.Count);
        Assert.Contains(result, d => d.TypeName == "Engineering" && d.TypeCode == "ENG");
        Assert.Contains(result, d => d.TypeName == "QA" && d.TypeCode == "QA");
        Assert.Contains(result, d => d.TypeName == "DevOps" && d.TypeCode == "DEVOPS");
        Assert.Contains(result, d => d.TypeName == "Product" && d.TypeCode == "PROD");
        Assert.Contains(result, d => d.TypeName == "Design" && d.TypeCode == "DESIGN");
    }
}
