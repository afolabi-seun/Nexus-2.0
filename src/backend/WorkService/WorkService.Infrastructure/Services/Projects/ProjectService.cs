using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.Projects;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.Projects;
using WorkService.Domain.Interfaces.Repositories.Stories;
using WorkService.Domain.Interfaces.Services.Outbox;
using WorkService.Domain.Interfaces.Services.Projects;
using WorkService.Domain.Results;
using WorkService.Infrastructure.Data;

namespace WorkService.Infrastructure.Services.Projects;

public partial class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepo;
    private readonly IStoryRepository _storyRepo;
    private readonly IOutboxService _outbox;
    private readonly WorkDbContext _dbContext;
    private readonly ILogger<ProjectService> _logger;

    [GeneratedRegex(@"^[A-Z0-9]{2,10}$")]
    private static partial Regex ProjectKeyRegex();

    public ProjectService(
        IProjectRepository projectRepo,
        IStoryRepository storyRepo,
        IOutboxService outbox,
        WorkDbContext dbContext,
        ILogger<ProjectService> logger)
    {
        _projectRepo = projectRepo;
        _storyRepo = storyRepo;
        _outbox = outbox;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ServiceResult<object>> CreateAsync(Guid organizationId, Guid creatorId, object request, CancellationToken ct = default)
    {
        var req = (CreateProjectRequest)request;

        if (!ProjectKeyRegex().IsMatch(req.ProjectKey))
            return ServiceResult<object>.Fail(
                ErrorCodes.ProjectKeyInvalidFormatValue, ErrorCodes.ProjectKeyInvalidFormat,
                $"Project key '{req.ProjectKey}' is invalid. Must be 2–10 uppercase alphanumeric characters.", 400);

        var existingByKey = await _projectRepo.GetByKeyAsync(req.ProjectKey, ct);
        if (existingByKey != null)
            return ServiceResult<object>.Fail(
                ErrorCodes.ProjectKeyDuplicateValue, ErrorCodes.ProjectKeyDuplicate,
                $"A project with key '{req.ProjectKey}' already exists.", 409);

        var existingByName = await _projectRepo.GetByNameAsync(organizationId, req.ProjectName, ct);
        if (existingByName != null)
            return ServiceResult<object>.Fail(
                ErrorCodes.ProjectNameDuplicateValue, ErrorCodes.ProjectNameDuplicate,
                $"A project with name '{req.ProjectName}' already exists in this organization.", 409);

        var project = new Project
        {
            OrganizationId = organizationId,
            ProjectName = req.ProjectName,
            ProjectKey = req.ProjectKey,
            Description = req.Description,
            LeadId = req.LeadId,
            FlgStatus = "A"
        };

        await _projectRepo.AddAsync(project, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _outbox.PublishAsync(new { MessageType = "AuditEvent", Action = "ProjectCreated", EntityType = "Project", EntityId = project.ProjectId.ToString(), OrganizationId = organizationId, UserId = creatorId }, ct);

        var detail = await BuildDetailResponse(project, ct);
        return ServiceResult<object>.Created(detail, "Project created successfully.");
    }

    public async Task<ServiceResult<object>> GetByIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        if (project == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.ProjectNotFoundValue, ErrorCodes.ProjectNotFound,
                $"Project with ID '{projectId}' was not found.", 404);

        var detail = await BuildDetailResponse(project, ct);
        return ServiceResult<object>.Ok(detail);
    }

    public async Task<ServiceResult<object>> ListAsync(Guid organizationId, int page, int pageSize, string? status, CancellationToken ct = default)
    {
        var (items, totalCount) = await _projectRepo.ListAsync(organizationId, page, pageSize, status, ct);
        var responses = new List<ProjectListResponse>();
        foreach (var p in items)
        {
            var storyCount = await _projectRepo.GetStoryCountAsync(p.ProjectId, ct);
            responses.Add(new ProjectListResponse
            {
                ProjectId = p.ProjectId,
                ProjectName = p.ProjectName,
                ProjectKey = p.ProjectKey,
                Description = p.Description,
                StoryCount = storyCount,
                FlgStatus = p.FlgStatus,
                DateCreated = p.DateCreated
            });
        }

        var paginated = new PaginatedResponse<ProjectListResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return ServiceResult<object>.Ok(paginated, "Projects retrieved.");
    }

    public async Task<ServiceResult<object>> UpdateAsync(Guid projectId, object request, CancellationToken ct = default)
    {
        var req = (UpdateProjectRequest)request;
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        if (project == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.ProjectNotFoundValue, ErrorCodes.ProjectNotFound,
                $"Project with ID '{projectId}' was not found.", 404);

        if (req.ProjectKey != null && req.ProjectKey != project.ProjectKey)
        {
            var hasStories = await _storyRepo.ExistsByProjectAsync(projectId, ct);
            if (hasStories)
                return ServiceResult<object>.Fail(
                    ErrorCodes.ProjectKeyImmutableValue, ErrorCodes.ProjectKeyImmutable,
                    $"Project key '{project.ProjectKey}' cannot be changed because stories already exist.", 400);

            if (!ProjectKeyRegex().IsMatch(req.ProjectKey))
                return ServiceResult<object>.Fail(
                    ErrorCodes.ProjectKeyInvalidFormatValue, ErrorCodes.ProjectKeyInvalidFormat,
                    $"Project key '{req.ProjectKey}' is invalid. Must be 2–10 uppercase alphanumeric characters.", 400);

            var existingByKey = await _projectRepo.GetByKeyAsync(req.ProjectKey, ct);
            if (existingByKey != null)
                return ServiceResult<object>.Fail(
                    ErrorCodes.ProjectKeyDuplicateValue, ErrorCodes.ProjectKeyDuplicate,
                    $"A project with key '{req.ProjectKey}' already exists.", 409);

            project.ProjectKey = req.ProjectKey;
        }

        if (req.ProjectName != null && req.ProjectName != project.ProjectName)
        {
            var existingByName = await _projectRepo.GetByNameAsync(project.OrganizationId, req.ProjectName, ct);
            if (existingByName != null)
                return ServiceResult<object>.Fail(
                    ErrorCodes.ProjectNameDuplicateValue, ErrorCodes.ProjectNameDuplicate,
                    $"A project with name '{req.ProjectName}' already exists in this organization.", 409);
            project.ProjectName = req.ProjectName;
        }

        if (req.Description != null) project.Description = req.Description;
        if (req.LeadId.HasValue) project.LeadId = req.LeadId;
        project.DateUpdated = DateTime.UtcNow;

        await _projectRepo.UpdateAsync(project, ct);
        await _dbContext.SaveChangesAsync(ct);

        var detail = await BuildDetailResponse(project, ct);
        return ServiceResult<object>.Ok(detail, "Project updated.");
    }

    public async Task<ServiceResult<object>> UpdateStatusAsync(Guid projectId, string newStatus, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct);
        if (project == null)
            return ServiceResult<object>.Fail(
                ErrorCodes.ProjectNotFoundValue, ErrorCodes.ProjectNotFound,
                $"Project with ID '{projectId}' was not found.", 404);

        project.FlgStatus = newStatus;
        project.DateUpdated = DateTime.UtcNow;
        await _projectRepo.UpdateAsync(project, ct);
        await _dbContext.SaveChangesAsync(ct);

        return ServiceResult<object>.NoContent("Project status updated.");
    }

    private async Task<ProjectDetailResponse> BuildDetailResponse(Project project, CancellationToken ct)
    {
        var storyCount = await _projectRepo.GetStoryCountAsync(project.ProjectId, ct);
        var sprintCount = await _projectRepo.GetSprintCountAsync(project.ProjectId, ct);

        return new ProjectDetailResponse
        {
            ProjectId = project.ProjectId,
            OrganizationId = project.OrganizationId,
            ProjectName = project.ProjectName,
            ProjectKey = project.ProjectKey,
            Description = project.Description,
            LeadId = project.LeadId,
            StoryCount = storyCount,
            SprintCount = sprintCount,
            FlgStatus = project.FlgStatus,
            DateCreated = project.DateCreated,
            DateUpdated = project.DateUpdated
        };
    }
}
