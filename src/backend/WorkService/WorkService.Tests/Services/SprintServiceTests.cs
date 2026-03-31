using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using WorkService.Application.DTOs.Sprints;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.SprintStories;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Infrastructure.Services.Sprints;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Tests.Services;

public class SprintServiceTests
{
    private readonly Mock<ISprintRepository> _sprintRepo = new();
    private readonly Mock<ISprintStoryRepository> _sprintStoryRepo = new();
    private readonly Mock<IStoryRepository> _storyRepo = new();
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _redisDb = new();
    private readonly Mock<ILogger<SprintService>> _logger = new();
    private readonly SprintService _sut;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    public SprintServiceTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisDb.Object);

        _sprintRepo.Setup(r => r.AddAsync(It.IsAny<Sprint>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sprint s, CancellationToken _) => s);
        _sprintStoryRepo.Setup(r => r.ListBySprintAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SprintStory>());

        var project = new Project { ProjectId = _projectId, OrganizationId = _orgId, ProjectKey = "PROJ" };
        _projectRepo.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _sut = new SprintService(
            _sprintRepo.Object, _sprintStoryRepo.Object,
            _storyRepo.Object, _taskRepo.Object, _projectRepo.Object,
            _outbox.Object, _redis.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_Succeeds()
    {
        var request = new CreateSprintRequest
        {
            SprintName = "Sprint 1",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        };

        var result = await _sut.CreateAsync(_orgId, _projectId, request);

        Assert.NotNull(result);
        _sprintRepo.Verify(r => r.AddAsync(It.Is<Sprint>(s => s.Status == "Planning"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EndDateBeforeStartDate_Throws()
    {
        var now = DateTime.UtcNow;
        var request = new CreateSprintRequest
        {
            SprintName = "Sprint 1",
            StartDate = now,
            EndDate = now.AddDays(-1)
        };

        await Assert.ThrowsAsync<SprintEndBeforeStartException>(
            () => _sut.CreateAsync(_orgId, _projectId, request));
    }

    [Fact]
    public async Task StartAsync_OneActiveSprintPerProject_Throws()
    {
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint
        {
            SprintId = sprintId, ProjectId = _projectId, Status = "Planning"
        };
        _sprintRepo.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _sprintRepo.Setup(r => r.GetActiveByProjectAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sprint { Status = "Active" });

        await Assert.ThrowsAsync<OnlyOneActiveSprintException>(
            () => _sut.StartAsync(sprintId));
    }

    [Fact]
    public async Task AddStoryAsync_ProjectMismatch_Throws()
    {
        var sprintId = Guid.NewGuid();
        var storyId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();

        var sprint = new Sprint { SprintId = sprintId, ProjectId = _projectId, Status = "Planning" };
        var story = new Story { StoryId = storyId, ProjectId = otherProjectId };

        _sprintRepo.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _storyRepo.Setup(r => r.GetByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        await Assert.ThrowsAsync<StoryProjectMismatchException>(
            () => _sut.AddStoryAsync(sprintId, storyId));
    }

    [Fact]
    public async Task CompleteAsync_CalculatesVelocity()
    {
        var sprintId = Guid.NewGuid();
        var sprint = new Sprint { SprintId = sprintId, ProjectId = _projectId, Status = "Active" };

        var story1Id = Guid.NewGuid();
        var story2Id = Guid.NewGuid();
        var sprintStories = new List<SprintStory>
        {
            new() { SprintId = sprintId, StoryId = story1Id },
            new() { SprintId = sprintId, StoryId = story2Id }
        };

        _sprintRepo.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);
        _sprintStoryRepo.Setup(r => r.ListBySprintAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprintStories);
        _storyRepo.Setup(r => r.GetByIdAsync(story1Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Story { StoryId = story1Id, ProjectId = _projectId, Status = "Done", StoryPoints = 5 });
        _storyRepo.Setup(r => r.GetByIdAsync(story2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Story { StoryId = story2Id, ProjectId = _projectId, Status = "InProgress", StoryPoints = 3 });
        _projectRepo.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { ProjectId = _projectId, ProjectKey = "PROJ" });

        await _sut.CompleteAsync(sprintId);

        // Velocity should be 5 (only the Done story's points)
        _sprintRepo.Verify(r => r.UpdateAsync(
            It.Is<Sprint>(s => s.Velocity == 5 && s.Status == "Completed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
