using System.Text.Json;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.StoryTemplates;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.StoryTemplates;
using WorkService.Domain.Interfaces.Services.StoryTemplates;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;
using Task = System.Threading.Tasks.Task;

namespace WorkService.Infrastructure.Services.StoryTemplates;

/// <summary>
/// Manages story template CRUD operations. Templates are org-scoped and soft-deleted.
/// </summary>
public class StoryTemplateService : IStoryTemplateService
{
    private readonly IStoryTemplateRepository _repo;
    private readonly WorkDbContext _dbContext;

    public StoryTemplateService(IStoryTemplateRepository repo, WorkDbContext dbContext)
    {
        _repo = repo;
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repo.ListByOrganizationAsync(organizationId, page, pageSize, ct);
        return ServiceResult<object>.Ok(new PaginatedResponse<StoryTemplateResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        }, "Story templates retrieved.");
    }

    public async Task<ServiceResult<object>> GetByIdAsync(Guid templateId, CancellationToken ct = default)
    {
        var template = await _repo.GetByIdAsync(templateId, ct)
            ?? throw new NotFoundException("StoryTemplate", templateId);
        return ServiceResult<object>.Ok(MapToResponse(template), "Story template retrieved.");
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid organizationId, object request, CancellationToken ct = default)
    {
        var req = (CreateStoryTemplateRequest)request;

        var existing = await _repo.GetByNameAsync(organizationId, req.Name, ct);
        if (existing != null)
            throw new ConflictException($"A template named '{req.Name}' already exists.");

        var entity = new StoryTemplate
        {
            OrganizationId = organizationId,
            Name = req.Name,
            Description = req.Description,
            DefaultTitle = req.DefaultTitle,
            DefaultDescription = req.DefaultDescription,
            DefaultAcceptanceCriteria = req.DefaultAcceptanceCriteria,
            DefaultPriority = req.DefaultPriority ?? "Medium",
            DefaultStoryType = req.DefaultStoryType ?? "Feature",
            DefaultStoryPoints = req.DefaultStoryPoints,
            DefaultLabelsJson = req.DefaultLabels != null ? JsonSerializer.Serialize(req.DefaultLabels) : null,
            DefaultTaskTypesJson = req.DefaultTaskTypes != null ? JsonSerializer.Serialize(req.DefaultTaskTypes) : null,
        };

        await _repo.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.Created(MapToResponse(entity), "Story template created.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid templateId, object request, CancellationToken ct = default)
    {
        var req = (UpdateStoryTemplateRequest)request;
        var template = await _repo.GetByIdAsync(templateId, ct)
            ?? throw new NotFoundException("StoryTemplate", templateId);

        if (req.Name != null) template.Name = req.Name;
        if (req.Description != null) template.Description = req.Description;
        if (req.DefaultTitle != null) template.DefaultTitle = req.DefaultTitle;
        if (req.DefaultDescription != null) template.DefaultDescription = req.DefaultDescription;
        if (req.DefaultAcceptanceCriteria != null) template.DefaultAcceptanceCriteria = req.DefaultAcceptanceCriteria;
        if (req.DefaultPriority != null) template.DefaultPriority = req.DefaultPriority;
        if (req.DefaultStoryType != null) template.DefaultStoryType = req.DefaultStoryType;
        if (req.DefaultStoryPoints.HasValue) template.DefaultStoryPoints = req.DefaultStoryPoints;
        if (req.DefaultLabels != null) template.DefaultLabelsJson = JsonSerializer.Serialize(req.DefaultLabels);
        if (req.DefaultTaskTypes != null) template.DefaultTaskTypesJson = JsonSerializer.Serialize(req.DefaultTaskTypes);
        template.DateUpdated = DateTime.UtcNow;

        await _repo.UpdateAsync(template, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.Ok(MapToResponse(template), "Story template updated.");
    }

    public async Task<ServiceResult<object>> DeleteAsync(Guid templateId, CancellationToken ct = default)
    {
        var template = await _repo.GetByIdAsync(templateId, ct)
            ?? throw new NotFoundException("StoryTemplate", templateId);

        template.IsActive = false;
        template.DateUpdated = DateTime.UtcNow;
        await _repo.UpdateAsync(template, ct);
        await _dbContext.SaveChangesAsync(ct);
        return ServiceResult<object>.NoContent("Story template deleted.");
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
        DefaultStoryType = t.DefaultStoryType,
        DefaultStoryPoints = t.DefaultStoryPoints,
        DefaultLabels = !string.IsNullOrEmpty(t.DefaultLabelsJson)
            ? JsonSerializer.Deserialize<List<string>>(t.DefaultLabelsJson) : null,
        DefaultTaskTypes = !string.IsNullOrEmpty(t.DefaultTaskTypesJson)
            ? JsonSerializer.Deserialize<List<string>>(t.DefaultTaskTypesJson) : null,
        DateCreated = t.DateCreated,
    };
}
