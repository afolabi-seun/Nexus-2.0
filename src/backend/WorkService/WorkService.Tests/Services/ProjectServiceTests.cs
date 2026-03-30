using Microsoft.Extensions.Logging;
using Moq;
using WorkService.Application.DTOs.Projects;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories;
using WorkService.Domain.Interfaces.Services;
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

        _sut = new ProjectService(_projectRepo.Object, _storyRepo.Object, _outbox.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_Succeeds()
    {
        var request = new CreateProjectRequest { ProjectName = "Test", ProjectKey = "TEST" };

        var result = await _sut.CreateAsync(_orgId, _creatorId, request);

        Assert.NotNull(result);
        _projectRepo.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateProjectKey_Throws()
    {
        _projectRepo.Setup(r => r.GetByKeyAsync("DUPE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { ProjectKey = "DUPE" });

        var request = new CreateProjectRequest { ProjectName = "Test", ProjectKey = "DUPE" };

        await Assert.ThrowsAsync<ProjectKeyDuplicateException>(
            () => _sut.CreateAsync(_orgId, _creatorId, request));
    }

    [Fact]
    public async Task CreateAsync_DuplicateProjectName_Throws()
    {
        _projectRepo.Setup(r => r.GetByNameAsync(_orgId, "Existing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { ProjectName = "Existing" });

        var request = new CreateProjectRequest { ProjectName = "Existing", ProjectKey = "NEW" };

        await Assert.ThrowsAsync<ProjectNameDuplicateException>(
            () => _sut.CreateAsync(_orgId, _creatorId, request));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("lower")]
    [InlineData("AB CD")]
    public async Task CreateAsync_InvalidProjectKeyFormat_Throws(string key)
    {
        var request = new CreateProjectRequest { ProjectName = "Test", ProjectKey = key };

        await Assert.ThrowsAsync<ProjectKeyInvalidFormatException>(
            () => _sut.CreateAsync(_orgId, _creatorId, request));
    }

    [Fact]
    public async Task UpdateAsync_ProjectKeyImmutable_WhenStoriesExist_Throws()
    {
        var projectId = Guid.NewGuid();
        var project = new Project { ProjectId = projectId, OrganizationId = _orgId, ProjectKey = "OLD", ProjectName = "P" };

        _projectRepo.Setup(r => r.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _storyRepo.Setup(r => r.ExistsByProjectAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new UpdateProjectRequest { ProjectKey = "NEW" };

        await Assert.ThrowsAsync<ProjectKeyImmutableException>(
            () => _sut.UpdateAsync(projectId, request));
    }
}
