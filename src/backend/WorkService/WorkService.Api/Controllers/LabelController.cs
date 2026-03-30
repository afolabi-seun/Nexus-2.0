using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Labels;
using WorkService.Domain.Interfaces.Services;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/labels")]
[Authorize]
public class LabelController : ControllerBase
{
    private readonly ILabelService _labelService;

    public LabelController(ILabelService labelService)
    {
        _labelService = labelService;
    }

    [HttpPost]
    [DeptLead]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateLabelRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _labelService.CreateAsync(orgId, request, ct);
        return StatusCode(201, Wrap(result, "Label created successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> List(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _labelService.ListAsync(orgId, ct);
        return Ok(Wrap(result, "Labels retrieved."));
    }

    [HttpPut("{id:guid}")]
    [DeptLead]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateLabelRequest request, CancellationToken ct)
    {
        var result = await _labelService.UpdateAsync(id, request, ct);
        return Ok(Wrap(result, "Label updated."));
    }

    [HttpDelete("{id:guid}")]
    [OrgAdmin]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _labelService.DeleteAsync(id, ct);
        return Ok(Wrap<object>(null!, "Label deleted."));
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);

    private ApiResponse<T> Wrap<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }

    private ApiResponse<object> Wrap(object data, string? message = null) => Wrap<object>(data, message);
}
