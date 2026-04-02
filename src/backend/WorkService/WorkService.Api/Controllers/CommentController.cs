using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
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
    public async Task<IActionResult> Create(
        [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var userId = GetUserId();
        var result = await _commentService.CreateAsync(orgId, userId, request, ct);
        return ApiResponse<object>.Ok(result, "Comment created successfully.").ToActionResult(HttpContext, 201);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _commentService.UpdateAsync(id, userId, request.Content, ct);
        return ApiResponse<object>.Ok(result, "Comment updated.").ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetRole();
        await _commentService.DeleteAsync(id, userId, role, ct);
        return ApiResponse<object>.Ok(null!, "Comment deleted.").ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
    private Guid GetUserId() => Guid.Parse(HttpContext.Items["userId"]?.ToString()!);
    private string GetRole() => HttpContext.Items["roleName"]?.ToString() ?? string.Empty;
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}
