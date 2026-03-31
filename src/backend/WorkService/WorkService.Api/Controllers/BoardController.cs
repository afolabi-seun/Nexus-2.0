using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Application.DTOs;
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
    public async Task<ActionResult<ApiResponse<object>>> GetKanbanBoard(
        [FromQuery] Guid? projectId = null, [FromQuery] Guid? sprintId = null,
        [FromQuery] Guid? departmentId = null, [FromQuery] Guid? assigneeId = null,
        [FromQuery] string? priority = null, [FromQuery] List<string>? labels = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _boardService.GetKanbanBoardAsync(orgId, projectId, sprintId, departmentId, assigneeId, priority, labels, ct);
        return Ok(Wrap(result));
    }

    [HttpGet("sprint")]
    public async Task<ActionResult<ApiResponse<object>>> GetSprintBoard(
        [FromQuery] Guid? projectId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _boardService.GetSprintBoardAsync(orgId, projectId, ct);
        return Ok(Wrap(result));
    }

    [HttpGet("backlog")]
    public async Task<ActionResult<ApiResponse<object>>> GetBacklog(
        [FromQuery] Guid? projectId = null, CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _boardService.GetBacklogAsync(orgId, projectId, ct);
        return Ok(Wrap(result));
    }

    [HttpGet("department")]
    public async Task<ActionResult<ApiResponse<object>>> GetDepartmentBoard(
        [FromQuery] Guid? projectId = null, [FromQuery] Guid? sprintId = null,
        CancellationToken ct = default)
    {
        var orgId = GetOrganizationId();
        var result = await _boardService.GetDepartmentBoardAsync(orgId, projectId, sprintId, ct);
        return Ok(Wrap(result));
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
