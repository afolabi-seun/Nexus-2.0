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

namespace WorkService.Infrastructure.Services.Projects;

public partial class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepo;
    private readonly IStoryRepository _storyRepo;
    private readonly IOutboxService _outbox;
    private readonly ILogger<ProjectService> _logger;

    [GeneratedRegex(@"^[A-Z0-9]{2,10}$")]
    private static partial Regex ProjectKeyRegex();

    public ProjectService(
        IProjectRepository projectRepo,
        IStoryRepository storyRepo,
        IOutboxService outbox,
        ILogger<ProjectService> logger)
    {
        _projectRepo = projectRepo;
        _storyRepo = storyRepo;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<object> CreateAsync(Guid organizationId, Guid creatorId, object request, CancellationToken ct = default)
    {
        var req = (CreateProjectRequest)request;

        if (!ProjectKeyRegex().IsMatch(req.ProjectKey))
            throw new ProjectKeyInvalidFormatException(req.ProjectKey);

        var existingByKey = await _projectRepo.GetByKeyAsync(req.ProjectKey, ct);
        if (existingByKey != null)
            throw new ProjectKeyDuplicateException(req.ProjectKey);

        var existingByName = await _projectRepo.GetByNameAsync(organizationId, req.ProjectName, ct);
        if (existingByName != null)
            throw new ProjectNameDuplicateException(req.ProjectName);

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

        await _outbox.PublishAsync(new { MessageType = "AuditEvent", Action = "ProjectCreated", EntityType = "Project", EntityId = project.ProjectId.ToString(), OrganizationId = organizationId, UserId = creatorId }, ct);

        return await BuildDetailResponse(project, ct);
    }

    public async Task<object> GetByIdAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct)
            ?? throw new ProjectNotFoundException(projectId);
        return await BuildDetailResponse(project, ct);
    }

    public async Task<object> ListAsync(Guid organizationId, int page, int pageSize, string? status, CancellationToken ct = default)
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

        return new PaginatedResponse<ProjectListResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task<object> UpdateAsync(Guid projectId, object request, CancellationToken ct = default)
    {
        var req = (UpdateProjectRequest)request;
        var project = await _projectRepo.GetByIdAsync(projectId, ct)
            ?? throw new ProjectNotFoundException(projectId);

        if (req.ProjectKey != null && req.ProjectKey != project.ProjectKey)
        {
            var hasStories = await _storyRepo.ExistsByProjectAsync(projectId, ct);
            if (hasStories) throw new ProjectKeyImmutableException(project.ProjectKey);

            if (!ProjectKeyRegex().IsMatch(req.ProjectKey))
                throw new ProjectKeyInvalidFormatException(req.ProjectKey);

            var existingByKey = await _projectRepo.GetByKeyAsync(req.ProjectKey, ct);
            if (existingByKey != null) throw new ProjectKeyDuplicateException(req.ProjectKey);

            project.ProjectKey = req.ProjectKey;
        }

        if (req.ProjectName != null && req.ProjectName != project.ProjectName)
        {
            var existingByName = await _projectRepo.GetByNameAsync(project.OrganizationId, req.ProjectName, ct);
            if (existingByName != null) throw new ProjectNameDuplicateException(req.ProjectName);
            project.ProjectName = req.ProjectName;
        }

        if (req.Description != null) project.Description = req.Description;
        if (req.LeadId.HasValue) project.LeadId = req.LeadId;
        project.DateUpdated = DateTime.UtcNow;

        await _projectRepo.UpdateAsync(project, ct);
        return await BuildDetailResponse(project, ct);
    }

    public async System.Threading.Tasks.Task UpdateStatusAsync(Guid projectId, string newStatus, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct)
            ?? throw new ProjectNotFoundException(projectId);
        project.FlgStatus = newStatus;
        project.DateUpdated = DateTime.UtcNow;
        await _projectRepo.UpdateAsync(project, ct);
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
