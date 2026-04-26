using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WorkService.Application.DTOs.Projects;
using WorkService.Domain.Entities;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;
using WorkService.Tests.Helpers;
using WorkService.Infrastructure.Services.Projects;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IStoryRepository> _storyRepo = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<ILogger<ProjectService>> _logger = new();
    private readonly ProjectService _sut;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _creatorId = Guid.NewGuid();

    public ProjectServiceTests()
    {
        _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);
        _projectRepo.Setup(r => r.GetStoryCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _projectRepo.Setup(r => r.GetSprintCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var dbContext = TestWorkDbContextFactory.Create();
        _sut = new ProjectService(_projectRepo.Object, _storyRepo.Object, _outbox.Object, dbContext, _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_Succeeds()
    {
        var request = new CreateProjectRequest { ProjectName = "Test", ProjectKey = "TEST" };

        var result = await _sut.CreateAsync(_orgId, _creatorId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Data);
        _projectRepo.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateProjectKey_ReturnsConflict()
    {
        _projectRepo.Setup(r => r.GetByKeyAsync("DUPE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { ProjectKey = "DUPE" });

        var request = new CreateProjectRequest { ProjectName = "Test", ProjectKey = "DUPE" };

        var result = await _sut.CreateAsync(_orgId, _creatorId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("PROJECT_KEY_DUPLICATE", result.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_DuplicateProjectName_ReturnsConflict()
    {
        _projectRepo.Setup(r => r.GetByNameAsync(_orgId, "Existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { ProjectName = "Existing" });

        var request = new CreateProjectRequest { ProjectName = "Existing", ProjectKey = "NEW" };

        var result = await _sut.CreateAsync(_orgId, _creatorId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("PROJECT_NAME_DUPLICATE", result.ErrorCode);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("lower")]
    [InlineData("AB CD")]
    public async Task CreateAsync_InvalidProjectKeyFormat_ReturnsBadRequest(string key)
    {
        var request = new CreateProjectRequest { ProjectName = "Test", ProjectKey = key };

        var result = await _sut.CreateAsync(_orgId, _creatorId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("PROJECT_KEY_INVALID_FORMAT", result.ErrorCode);
    }

    [Fact]
    public async Task UpdateAsync_ProjectKeyImmutable_WhenStoriesExist_ReturnsBadRequest()
    {
        var projectId = Guid.NewGuid();
        var project = new Project { ProjectId = projectId, OrganizationId = _orgId, ProjectKey = "OLD", ProjectName = "P" };

        _projectRepo.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _storyRepo.Setup(r => r.ExistsByProjectAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateProjectRequest { ProjectKey = "NEW" };

        var result = await _sut.UpdateAsync(projectId, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("PROJECT_KEY_IMMUTABLE", result.ErrorCode);
    }
}
