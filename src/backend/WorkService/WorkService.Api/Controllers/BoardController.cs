using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
using WorkService.Domain.Interfaces.Services.Boards;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/boards")]
[Authorize]
public class BoardController : ControllerBase
{
    private readonly IBoardService _boardService;

    public BoardController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    [HttpGet("kanban")]
    public async Task<IActionResult> GetKanbanBoard(
        [FromQuery] Guid? projectId = null, [FromQuery] Guid? sprintId = null,
        [FromQuery] Guid? departmentId = null, [FromQuery] Guid? assigneeId = null,
        [FromQuery] string? priority = null, [FromQuery] List<string>? labels = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        return (await _boardService.GetKanbanBoardAsync(orgId, projectId, sprintId, departmentId, assigneeId, priority, labels, ct)).ToActionResult(HttpContext);
    }

    [HttpGet("sprint")]
    public async Task<IActionResult> GetSprintBoard(
        [FromQuery] Guid? projectId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        return (await _boardService.GetSprintBoardAsync(orgId, projectId, ct)).ToActionResult(HttpContext);
    }

    [HttpGet("backlog")]
    public async Task<IActionResult> GetBacklog(
        [FromQuery] Guid? projectId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        return (await _boardService.GetBacklogAsync(orgId, projectId, ct)).ToActionResult(HttpContext);
    }

    [HttpGet("department")]
    public async Task<IActionResult> GetDepartmentBoard(
        [FromQuery] Guid? projectId = null, [FromQuery] Guid? sprintId = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        return (await _boardService.GetDepartmentBoardAsync(orgId, projectId, sprintId, ct)).ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
}
