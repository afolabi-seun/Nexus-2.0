using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Interfaces.Services.Roles;

namespace ProfileService.Api.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _roleService.ListAsync(ct);
        return ApiResponse<object>.Ok(result, "Roles retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _roleService.GetByIdAsync(id, ct);
        return ApiResponse<object>.Ok(result).ToActionResult(HttpContext);
    }
}
