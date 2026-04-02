using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Attributes;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Labels;
using WorkService.Domain.Interfaces.Services.Labels;

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
    public async Task<IActionResult> Create(
        [FromBody] CreateLabelRequest request, CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _labelService.CreateAsync(orgId, request, ct);
        return ApiResponse<object>.Ok(result, "Label created successfully.").ToActionResult(HttpContext, 201);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var orgId = GetOrganizationId();
        var result = await _labelService.ListAsync(orgId, ct);
        return ApiResponse<object>.Ok(result, "Labels retrieved.").ToActionResult(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [DeptLead]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateLabelRequest request, CancellationToken ct)
    {
        var result = await _labelService.UpdateAsync(id, request, ct);
        return ApiResponse<object>.Ok(result, "Label updated.").ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [OrgAdmin]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _labelService.DeleteAsync(id, ct);
        return ApiResponse<object>.Ok(null!, "Label deleted.").ToActionResult(HttpContext);
    }

    private Guid GetOrganizationId() => Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
}
