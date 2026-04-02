using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WorkService.Application.DTOs.Stories;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.ActivityLogs;
using WorkService.Domain.Interfaces.Repositories.Comments;
using WorkService.Domain.Interfaces.Repositories.Labels;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.Sprints;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.StoryLabels;
using WorkService.Domain.Interfaces.Repositories.StoryLinks;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Domain.Interfaces.Services.Stories;
using WorkService.Infrastructure.Data;
using WorkService.Tests.Helpers;
using WorkService.Infrastructure.Services.Stories;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Tests.Services;

public class StoryServiceTests
{
    private readonly Mock<IStoryRepository> _storyRepo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly Mock<ICommentRepository> _commentRepo = new();
    private readonly Mock<IStoryLabelRepository> _storyLabelRepo = new();
    private readonly Mock<ILabelRepository> _labelRepo = new();
    private readonly Mock<IStoryLinkRepository> _storyLinkRepo = new();
    private readonly Mock<ISprintRepository> _sprintRepo = new();
    private readonly Mock<IActivityLogRepository> _activityLogRepo = new();
    private readonly Mock<IStoryIdGenerator> _storyIdGen = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<ILogger<StoryService>> _logger = new();
    private readonly StoryService _sut;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _reporterId = Guid.NewGuid();
    private readonly Guid _projectId = Guid.NewGuid();

    public StoryServiceTests()
    {
        _storyRepo.Setup(r => r.AddAsync(It.IsAny<Story>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Story s, CancellationToken _) => s);
        _storyRepo.Setup(r => r.CountTasksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _storyRepo.Setup(r => r.CountCompletedTasksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _taskRepo.Setup(r => r.ListByStoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Domain.Entities.Task>());
        _storyLabelRepo.Setup(r => r.ListByStoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<StoryLabel>());
        _storyLinkRepo.Setup(r => r.ListByStoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<StoryLink>());
        _commentRepo.Setup(r => r.ListByEntityAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Comment>());
        _activityLogRepo.Setup(r => r.AddAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivityLog a, CancellationToken _) => a);

        var project = new Project { ProjectId = _projectId, OrganizationId = _orgId, ProjectKey = "NEXUS" };
        _projectRepo.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        _storyIdGen.Setup(g => g.GenerateNextIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("NEXUS-1", 1L));

        var dbContext = TestWorkDbContextFactory.Create();
        _sut = new StoryService(
            _storyRepo.Object, _projectRepo.Object, _taskRepo.Object,
            _commentRepo.Object, _storyLabelRepo.Object, _labelRepo.Object,
            _storyLinkRepo.Object, _sprintRepo.Object,
            _activityLogRepo.Object, _storyIdGen.Object,
            _outbox.Object, dbContext, _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_GeneratesCorrectKeyFormat()
    {
        var request = new CreateStoryRequest
        {
            ProjectId = _projectId, Title = "Test Story", Priority = "High"
        };

        var result = await _sut.CreateAsync(_orgId, _reporterId, request);

        Assert.NotNull(result);
        // The story ID generator was called with the correct project ID
        _storyIdGen.Verify(g => g.GenerateNextIdAsync(_projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidFibonacciPoints_Throws()
    {
        var request = new CreateStoryRequest
        {
            ProjectId = _projectId, Title = "Test", Priority = "Medium", StoryPoints = 4
        };

        await Assert.ThrowsAsync<InvalidStoryPointsException>(
            () => _sut.CreateAsync(_orgId, _reporterId, request));
    }

    [Fact]
    public async Task TransitionStatusAsync_ValidTransition_Succeeds()
    {
        var storyId = Guid.NewGuid();
        var story = new Story
        {
            StoryId = storyId, OrganizationId = _orgId, ProjectId = _projectId,
            StoryKey = "NEXUS-1", Status = "Backlog",
            Description = "Has description", StoryPoints = 5
        };
        _storyRepo.Setup(r => r.GetByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        var result = await _sut.TransitionStatusAsync(storyId, _reporterId, "Ready");

        Assert.NotNull(result);
        _storyRepo.Verify(r => r.UpdateAsync(It.Is<Story>(s => s.Status == "Ready"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransitionStatusAsync_InvalidTransition_Throws()
    {
        var storyId = Guid.NewGuid();
        var story = new Story
        {
            StoryId = storyId, OrganizationId = _orgId, ProjectId = _projectId,
            StoryKey = "NEXUS-1", Status = "Backlog"
        };
        _storyRepo.Setup(r => r.GetByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        await Assert.ThrowsAsync<InvalidStoryTransitionException>(
            () => _sut.TransitionStatusAsync(storyId, _reporterId, "Done"));
    }

    [Fact]
    public async Task TransitionToReady_WithoutDescription_Throws()
    {
        var storyId = Guid.NewGuid();
        var story = new Story
        {
            StoryId = storyId, OrganizationId = _orgId, ProjectId = _projectId,
            StoryKey = "NEXUS-1", Status = "Backlog",
            Description = null, StoryPoints = 5
        };
        _storyRepo.Setup(r => r.GetByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        await Assert.ThrowsAsync<StoryDescriptionRequiredException>(
            () => _sut.TransitionStatusAsync(storyId, _reporterId, "Ready"));
    }

    [Fact]
    public async Task TransitionToInProgress_WithoutAssignee_Throws()
    {
        var storyId = Guid.NewGuid();
        var story = new Story
        {
            StoryId = storyId, OrganizationId = _orgId, ProjectId = _projectId,
            StoryKey = "NEXUS-1", Status = "Ready",
            AssigneeId = null
        };
        _storyRepo.Setup(r => r.GetByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        await Assert.ThrowsAsync<StoryRequiresAssigneeException>(
            () => _sut.TransitionStatusAsync(storyId, _reporterId, "InProgress"));
    }
}
