using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Application.DTOs;
using WorkService.Domain.Interfaces.Services.Tasks;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/stories/{storyId:guid}/tasks")]
[Authorize]
public class StoryTaskController : ControllerBase
{
    private readonly ITaskService _taskService;

    public StoryTaskController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> ListByStory(Guid storyId, CancellationToken ct)
    {
        var result = await _taskService.ListByStoryAsync(storyId, ct);
        var response = ApiResponse<object>.Ok(result, "Tasks retrieved.");
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return Ok(response);
    }
}
