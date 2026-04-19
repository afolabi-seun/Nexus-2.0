using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkService.Api.Extensions;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.StoryTemplates;
using WorkService.Application.Helpers;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.StoryTemplates;
using WorkService.Infrastructure.Data;

namespace WorkService.Api.Controllers;

[ApiController]
[Route("api/v1/story-templates")]
[Authorize]
public class StoryTemplateController : ControllerBase
{
    private readonly IStoryTemplateRepository _repo;
    private readonly WorkDbContext _dbContext;

    public StoryTemplateController(IStoryTemplateRepository repo, WorkDbContext dbContext)
    {
        _repo = repo;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        PaginationHelper.Normalize(ref page, ref pageSize);
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);
        var (items, totalCount) = await _repo.ListByOrganizationAsync(orgId, page, pageSize, ct);

        var responses = items.Select(MapToResponse).ToList();
        var result = new PaginatedResponse<StoryTemplateResponse>
        {
            Data = responses, TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return ApiResponse<object>.Ok(result, "Story templates retrieved.").ToActionResult(HttpContext);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var template = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("StoryTemplate", id);
        return ApiResponse<object>.Ok(MapToResponse(template)).ToActionResult(HttpContext);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStoryTemplateRequest request, CancellationToken ct)
    {
        var orgId = Guid.Parse(HttpContext.Items["organizationId"]?.ToString()!);

        var existing = await _repo.GetByNameAsync(orgId, request.Name, ct);
        if (existing != null)
            throw new ConflictException($"A template named '{request.Name}' already exists.");

        var entity = new StoryTemplate
        {
            OrganizationId = orgId,
            Name = request.Name,
            Description = request.Description,
            DefaultTitle = request.DefaultTitle,
            DefaultDescription = request.DefaultDescription,
            DefaultAcceptanceCriteria = request.DefaultAcceptanceCriteria,
            DefaultPriority = request.DefaultPriority,
            DefaultStoryPoints = request.DefaultStoryPoints,
            DefaultLabelsJson = request.DefaultLabels != null ? JsonSerializer.Serialize(request.DefaultLabels) : null,
            DefaultTaskTypesJson = request.DefaultTaskTypes != null ? JsonSerializer.Serialize(request.DefaultTaskTypes) : null,
        };

        await _repo.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(MapToResponse(entity), "Story template created.").ToActionResult(HttpContext, 201);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateStoryTemplateRequest request, CancellationToken ct)
    {
        var template = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("StoryTemplate", id);

        if (request.Name != null) template.Name = request.Name;
        if (request.Description != null) template.Description = request.Description;
        if (request.DefaultTitle != null) template.DefaultTitle = request.DefaultTitle;
        if (request.DefaultDescription != null) template.DefaultDescription = request.DefaultDescription;
        if (request.DefaultAcceptanceCriteria != null) template.DefaultAcceptanceCriteria = request.DefaultAcceptanceCriteria;
        if (request.DefaultPriority != null) template.DefaultPriority = request.DefaultPriority;
        if (request.DefaultStoryPoints.HasValue) template.DefaultStoryPoints = request.DefaultStoryPoints;
        if (request.DefaultLabels != null) template.DefaultLabelsJson = JsonSerializer.Serialize(request.DefaultLabels);
        if (request.DefaultTaskTypes != null) template.DefaultTaskTypesJson = JsonSerializer.Serialize(request.DefaultTaskTypes);
        template.DateUpdated = DateTime.UtcNow;

        await _repo.UpdateAsync(template, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(MapToResponse(template), "Story template updated.").ToActionResult(HttpContext);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var template = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("StoryTemplate", id);

        template.IsActive = false;
        template.DateUpdated = DateTime.UtcNow;
        await _repo.UpdateAsync(template, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(null!, "Story template deleted.").ToActionResult(HttpContext);
    }

    private static StoryTemplateResponse MapToResponse(StoryTemplate t) => new()
    {
        StoryTemplateId = t.StoryTemplateId,
        Name = t.Name,
        Description = t.Description,
        DefaultTitle = t.DefaultTitle,
        DefaultDescription = t.DefaultDescription,
        DefaultAcceptanceCriteria = t.DefaultAcceptanceCriteria,
        DefaultPriority = t.DefaultPriority,
        DefaultStoryPoints = t.DefaultStoryPoints,
        DefaultLabels = !string.IsNullOrEmpty(t.DefaultLabelsJson)
            ? JsonSerializer.Deserialize<List<string>>(t.DefaultLabelsJson) : null,
        DefaultTaskTypes = !string.IsNullOrEmpty(t.DefaultTaskTypesJson)
            ? JsonSerializer.Deserialize<List<string>>(t.DefaultTaskTypesJson) : null,
        DateCreated = t.DateCreated,
    };
}
