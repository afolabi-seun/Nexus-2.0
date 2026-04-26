using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
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
    public async Task<IActionResult> ListByStory(Guid storyId, CancellationToken ct)
    {
        return (await _taskService.ListByStoryAsync(storyId, ct)).ToActionResult(HttpContext);
    }
}
