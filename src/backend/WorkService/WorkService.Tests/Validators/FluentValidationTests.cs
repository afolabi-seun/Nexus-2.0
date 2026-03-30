using FluentValidation.TestHelper;
using WorkService.Application.DTOs.Labels;
using WorkService.Application.DTOs.Projects;
using WorkService.Application.DTOs.Search;
using WorkService.Application.DTOs.Sprints;
using WorkService.Application.DTOs.Stories;
using WorkService.Application.DTOs.Tasks;
using WorkService.Application.Validators;

namespace WorkService.Tests.Validators;

public class FluentValidationTests
{
    // --- CreateProjectRequestValidator ---

    [Fact]
    public void CreateProject_ValidRequest_Passes()
    {
        var validator = new CreateProjectRequestValidator();
        var result = validator.TestValidate(new CreateProjectRequest
        {
            ProjectName = "My Project",
            ProjectKey = "PROJ"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("proj")]       // lowercase
    [InlineData("A")]          // too short
    [InlineData("ABCDEFGHIJK")] // too long (11 chars)
    [InlineData("AB CD")]      // spaces
    [InlineData("AB-CD")]      // special chars
    public void CreateProject_InvalidProjectKey_Fails(string key)
    {
        var validator = new CreateProjectRequestValidator();
        var result = validator.TestValidate(new CreateProjectRequest
        {
            ProjectName = "My Project",
            ProjectKey = key
        });
        result.ShouldHaveValidationErrorFor(x => x.ProjectKey);
    }

    [Fact]
    public void CreateProject_EmptyName_Fails()
    {
        var validator = new CreateProjectRequestValidator();
        var result = validator.TestValidate(new CreateProjectRequest
        {
            ProjectName = "",
            ProjectKey = "PROJ"
        });
        result.ShouldHaveValidationErrorFor(x => x.ProjectName);
    }

    // --- CreateStoryRequestValidator ---

    [Fact]
    public void CreateStory_ValidRequest_Passes()
    {
        var validator = new CreateStoryRequestValidator();
        var result = validator.TestValidate(new CreateStoryRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "A story",
            Priority = "High",
            StoryPoints = 5
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateStory_NonFibonacciPoints_Fails(int points)
    {
        var validator = new CreateStoryRequestValidator();
        var result = validator.TestValidate(new CreateStoryRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "A story",
            Priority = "Medium",
            StoryPoints = points
        });
        result.ShouldHaveValidationErrorFor(x => x.StoryPoints);
    }

    [Theory]
    [InlineData("Urgent")]
    [InlineData("low")]
    [InlineData("")]
    public void CreateStory_InvalidPriority_Fails(string priority)
    {
        var validator = new CreateStoryRequestValidator();
        var result = validator.TestValidate(new CreateStoryRequest
        {
            ProjectId = Guid.NewGuid(),
            Title = "A story",
            Priority = priority
        });
        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    // --- CreateTaskRequestValidator ---

    [Fact]
    public void CreateTask_ValidRequest_Passes()
    {
        var validator = new CreateTaskRequestValidator();
        var result = validator.TestValidate(new CreateTaskRequest
        {
            StoryId = Guid.NewGuid(),
            Title = "A task",
            TaskType = "Development",
            Priority = "High"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("InvalidType")]
    [InlineData("")]
    [InlineData("development")]
    public void CreateTask_InvalidTaskType_Fails(string taskType)
    {
        var validator = new CreateTaskRequestValidator();
        var result = validator.TestValidate(new CreateTaskRequest
        {
            StoryId = Guid.NewGuid(),
            Title = "A task",
            TaskType = taskType,
            Priority = "Medium"
        });
        result.ShouldHaveValidationErrorFor(x => x.TaskType);
    }

    // --- CreateSprintRequestValidator ---

    [Fact]
    public void CreateSprint_ValidRequest_Passes()
    {
        var validator = new CreateSprintRequestValidator();
        var result = validator.TestValidate(new CreateSprintRequest
        {
            SprintName = "Sprint 1",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateSprint_EndDateBeforeStartDate_Fails()
    {
        var validator = new CreateSprintRequestValidator();
        var now = DateTime.UtcNow;
        var result = validator.TestValidate(new CreateSprintRequest
        {
            SprintName = "Sprint 1",
            StartDate = now,
            EndDate = now.AddDays(-1)
        });
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    // --- LogHoursRequestValidator ---

    [Fact]
    public void LogHours_PositiveHours_Passes()
    {
        var validator = new LogHoursRequestValidator();
        var result = validator.TestValidate(new LogHoursRequest { Hours = 2.5m });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void LogHours_ZeroOrNegative_Fails(decimal hours)
    {
        var validator = new LogHoursRequestValidator();
        var result = validator.TestValidate(new LogHoursRequest { Hours = hours });
        result.ShouldHaveValidationErrorFor(x => x.Hours);
    }

    // --- CreateLabelRequestValidator ---

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#00ff00")]
    [InlineData("#abcdef")]
    public void CreateLabel_ValidHexColor_Passes(string color)
    {
        var validator = new CreateLabelRequestValidator();
        var result = validator.TestValidate(new CreateLabelRequest
        {
            Name = "Bug",
            Color = color
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#FFF")]
    [InlineData("FF0000")]
    [InlineData("#GGGGGG")]
    [InlineData("")]
    public void CreateLabel_InvalidHexColor_Fails(string color)
    {
        var validator = new CreateLabelRequestValidator();
        var result = validator.TestValidate(new CreateLabelRequest
        {
            Name = "Bug",
            Color = color
        });
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }

    // --- SearchRequestValidator ---

    [Fact]
    public void Search_QueryMinLength2_Passes()
    {
        var validator = new SearchRequestValidator();
        var result = validator.TestValidate(new SearchRequest { Query = "ab", Page = 1, PageSize = 20 });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Search_QueryTooShort_Fails()
    {
        var validator = new SearchRequestValidator();
        var result = validator.TestValidate(new SearchRequest { Query = "a", Page = 1, PageSize = 20 });
        result.ShouldHaveValidationErrorFor(x => x.Query);
    }
}
