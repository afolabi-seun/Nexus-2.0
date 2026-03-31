using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProfileService.Api.Attributes;
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
    public async Task<ActionResult<ApiResponse<object>>> GetNavigation(CancellationToken ct)
    {
        var roleName = HttpContext.Items["roleName"]?.ToString() ?? "Viewer";
        var roleObj = await _roleService.GetByNameAsync(roleName, ct);
        var permissionLevel = roleObj is Application.DTOs.Roles.RoleResponse role ? role.PermissionLevel : 25;

        var items = await _navigationService.GetNavigationAsync(permissionLevel, ct);
        return Ok(Wrap(items.Select(MapToResponse).ToList()));
    }

    [HttpGet("all")]
    [ServiceAuth]
    public async Task<ActionResult<ApiResponse<object>>> GetAll(CancellationToken ct)
    {
        var items = await _navigationService.GetAllNavigationItemsAsync(ct);
        return Ok(Wrap(items.Select(MapToResponse).ToList()));
    }

    [HttpPost]
    [ServiceAuth]
    public async Task<ActionResult<ApiResponse<object>>> Create(
        [FromBody] CreateNavigationItemRequest request, CancellationToken ct)
    {
        var entity = new NavigationItem
        {
            Label = request.Label,
            Path = request.Path,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            ParentId = request.ParentId,
            MinPermissionLevel = request.MinPermissionLevel,
            IsEnabled = request.IsEnabled,
        };
        var created = await _navigationService.CreateAsync(entity, ct);
        return Ok(Wrap(MapToResponse(created), "Navigation item created."));
    }

    [HttpPut("{id:guid}")]
    [ServiceAuth]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateNavigationItemRequest request, CancellationToken ct)
    {
        var entity = new NavigationItem { NavigationItemId = id };
        if (request.Label != null) entity.Label = request.Label;
        if (request.Path != null) entity.Path = request.Path;
        if (request.Icon != null) entity.Icon = request.Icon;
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
        if (request.MinPermissionLevel.HasValue) entity.MinPermissionLevel = request.MinPermissionLevel.Value;
        if (request.IsEnabled.HasValue) entity.IsEnabled = request.IsEnabled.Value;

        var updated = await _navigationService.UpdateAsync(entity, ct);
        return Ok(Wrap(MapToResponse(updated), "Navigation item updated."));
    }

    [HttpDelete("{id:guid}")]
    [ServiceAuth]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _navigationService.DeleteAsync(id, ct);
        return Ok(Wrap<object>(null!, "Navigation item deleted."));
    }

    private static NavigationItemResponse MapToResponse(NavigationItem item)
    {
        return new NavigationItemResponse
        {
            NavigationItemId = item.NavigationItemId,
            Label = item.Label,
            Path = item.Path,
            Icon = item.Icon,
            SortOrder = item.SortOrder,
            ParentId = item.ParentId,
            MinPermissionLevel = item.MinPermissionLevel,
            IsEnabled = item.IsEnabled,
            Children = item.Children.Select(MapToResponse).ToList(),
        };
    }

    private ApiResponse<T> Wrap<T>(T data, string? message = null)
    {
        var response = ApiResponse<T>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }

    private ApiResponse<object> Wrap(object data, string? message = null)
    {
        var response = ApiResponse<object>.Ok(data, message);
        response.CorrelationId = HttpContext.Items["CorrelationId"]?.ToString();
        return response;
    }
}
