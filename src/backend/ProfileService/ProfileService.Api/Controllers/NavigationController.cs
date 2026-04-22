using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
using ProfileService.Api.Extensions;
using ProfileService.Application.DTOs;
using ProfileService.Application.DTOs.Navigation;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Interfaces.Services.Navigation;
using ProfileService.Domain.Interfaces.Services.Roles;

namespace ProfileService.Api.Controllers;

[ApiController]
[Route("api/v1/navigation")]
[Authorize]
public class NavigationController : ControllerBase
{
    private readonly INavigationService _navigationService;
    private readonly IRoleService _roleService;

    public NavigationController(INavigationService navigationService, IRoleService roleService)
    {
        _navigationService = navigationService;
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNavigation(CancellationToken ct)
    {
        var roleName = HttpContext.Items["roleName"]?.ToString() ?? "Viewer";
        var roleObj = await _roleService.GetByNameAsync(roleName, ct);
        var permissionLevel = roleObj is Application.DTOs.Roles.RoleResponse role ? role.PermissionLevel : 25;

        return (await _navigationService.GetNavigationAsync(permissionLevel, ct)).ToActionResult(HttpContext);
    }

    [HttpGet("all")]
    [PlatformAdmin]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return (await _navigationService.GetAllNavigationItemsAsync(ct)).ToActionResult(HttpContext);
    }

    [HttpPost]
    [PlatformAdmin]
    public async Task<IActionResult> Create(
        [FromBody] CreateNavigationItemRequest request, CancellationToken ct)
    {
        var entity = new NavigationItem
        {
            Label = request.Label,
            Path = request.Path,
            Icon = request.Icon,
            Section = request.Section,
            SortOrder = request.SortOrder,
            ParentId = request.ParentId,
            MinPermissionLevel = request.MinPermissionLevel,
            IsEnabled = request.IsEnabled,
        };
        return (await _navigationService.CreateAsync(entity, ct)).ToActionResult(HttpContext);
    }

    [HttpPut("{id:guid}")]
    [PlatformAdmin]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateNavigationItemRequest request, CancellationToken ct)
    {
        var entity = new NavigationItem { NavigationItemId = id };
        if (request.Label != null) entity.Label = request.Label;
        if (request.Path != null) entity.Path = request.Path;
        if (request.Icon != null) entity.Icon = request.Icon;
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
        if (request.MinPermissionLevel.HasValue) entity.MinPermissionLevel = request.MinPermissionLevel.Value;
        if (request.IsEnabled.HasValue) entity.IsEnabled = request.IsEnabled.Value;

        return (await _navigationService.UpdateAsync(entity, ct)).ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    [PlatformAdmin]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return (await _navigationService.DeleteAsync(id, ct)).ToActionResult(HttpContext);
    }
}
