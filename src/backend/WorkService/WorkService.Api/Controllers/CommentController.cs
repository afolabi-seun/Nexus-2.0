using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Comments;
using WorkService.Domain.Interfaces.Services.Comments;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/comments")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var result = await _commentService.CreateAsync(orgId, userId, request, ct);
        return StatusCode(201, Wrap(result, "Comment created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _commentService.UpdateAsync(id, userId, request.Content, ct);
        return Ok(Wrap(result, "Comment updated."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetRole();
        await _commentService.DeleteAsync(id, userId, role, ct);
        return Ok(Wrap<object>(null!, "Comment deleted."));
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
    private string GetRole() => HttpContext.Items["roleName"]?.ToString() ?? string.Empty;

    private ApiResponse<T> Wrap<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => Wrap<object>(data, message);
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
