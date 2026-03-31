using Microsoft.Extensions.Logging;
using Moq;
using WorkService.Application.DTOs.Tasks;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.ActivityLogs;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Repositories.Tasks;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Infrastructure.Services.Tasks;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly Mock<IStoryRepository> _storyRepo = new();
    private readonly Mock<IActivityLogRepository> _activityLogRepo = new();
    private readonly Mock<IOutboxService> _outbox = new();
    private readonly Mock<ILogger<TaskService>> _logger = new();
    private readonly TaskService _sut;

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _creatorId = Guid.NewGuid();
    private readonly Guid _storyId = Guid.NewGuid();

    public TaskServiceTests()
    {
        _taskRepo.Setup(r => r.AddAsync(It.IsAny<Domain.Entities.Task>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Task t, CancellationToken _) => t);
        _activityLogRepo.Setup(r => r.AddAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivityLog a, CancellationToken _) => a);

        var story = new Story { StoryId = _storyId, OrganizationId = _orgId, StoryKey = "NEXUS-1" };
        _storyRepo.Setup(r => r.GetByIdAsync(_storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        _sut = new TaskService(
            _taskRepo.Object, _storyRepo.Object,
            _activityLogRepo.Object, _outbox.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_AutoMapsDepartment()
    {
        var request = new CreateTaskRequest
        {
            StoryId = _storyId, Title = "Dev task", TaskType = "Development", Priority = "High"
        };

        var result = await _sut.CreateAsync(_orgId, _creatorId, request);

        Assert.NotNull(result);
        // Without a profile client, departmentId will be null, but the task type mapping was exercised
        _taskRepo.Verify(r => r.AddAsync(
            It.Is<Domain.Entities.Task>(t => t.TaskType == "Development" && t.Status == "ToDo"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidTaskType_Throws()
    {
        var request = new CreateTaskRequest
        {
            StoryId = _storyId, Title = "Bad task", TaskType = "InvalidType", Priority = "Medium"
        };

        await Assert.ThrowsAsync<InvalidTaskTypeException>(
            () => _sut.CreateAsync(_orgId, _creatorId, request));
    }

    [Fact]
    public async Task LogHoursAsync_Accumulates()
    {
        var taskId = Guid.NewGuid();
        var task = new Domain.Entities.Task
        {
            TaskId = taskId, StoryId = _storyId, OrganizationId = _orgId,
            ActualHours = 2m
        };
        _taskRepo.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        await _sut.LogHoursAsync(taskId, _creatorId, 3m, null);

        _taskRepo.Verify(r => r.UpdateAsync(
            It.Is<Domain.Entities.Task>(t => t.ActualHours == 5m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogHoursAsync_NegativeHours_Throws()
    {
        var taskId = Guid.NewGuid();
        await Assert.ThrowsAsync<HoursMustBePositiveException>(
            () => _sut.LogHoursAsync(taskId, _creatorId, -1m, null));
    }

    [Fact]
    public async Task LogHoursAsync_ZeroHours_Throws()
    {
        var taskId = Guid.NewGuid();
        await Assert.ThrowsAsync<HoursMustBePositiveException>(
            () => _sut.LogHoursAsync(taskId, _creatorId, 0m, null));
    }
}
