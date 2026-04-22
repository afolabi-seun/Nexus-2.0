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
        return (await _roleService.ListAsync(ct)).ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return (await _roleService.GetByIdAsync(id, ct)).ToActionResult(HttpContext);
    }
}
