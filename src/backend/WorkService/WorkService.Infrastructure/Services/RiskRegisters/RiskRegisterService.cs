using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs;
using WorkService.Application.DTOs.RiskRegisters;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.RiskRegisters;
using WorkService.Domain.Interfaces.Services.RiskRegisters;

namespace WorkService.Infrastructure.Services.RiskRegisters;

public class RiskRegisterService : IRiskRegisterService
{
    private static readonly HashSet<string> ValidSeverities = new() { "Low", "Medium", "High", "Critical" };
    private static readonly HashSet<string> ValidLikelihoods = new() { "Low", "Medium", "High" };
    private static readonly HashSet<string> ValidMitigationStatuses = new() { "Open", "Mitigating", "Mitigated", "Accepted" };

    private readonly IRiskRegisterRepository _riskRepo;
    private readonly ILogger<RiskRegisterService> _logger;

    public RiskRegisterService(
        IRiskRegisterRepository riskRepo,
        ILogger<RiskRegisterService> logger)
    {
        _riskRepo = riskRepo;
        _logger = logger;
    }

    public async Task<object> CreateAsync(Guid orgId, Guid userId, object request, CancellationToken ct = default)
    {
        var req = (CreateRiskRequest)request;

        ValidateEnums(req.Severity, req.Likelihood, req.MitigationStatus);

        var risk = new RiskRegister
        {
            OrganizationId = orgId,
            ProjectId = req.ProjectId,
            SprintId = req.SprintId,
            Title = req.Title,
            Description = req.Description,
            Severity = req.Severity,
            Likelihood = req.Likelihood,
            MitigationStatus = req.MitigationStatus,
            CreatedBy = userId,
            FlgStatus = "A"
        };

        await _riskRepo.AddAsync(risk, ct);
        _logger.LogInformation("Created risk register entry {RiskId} for project {ProjectId}", risk.RiskRegisterId, risk.ProjectId);

        return MapToResponse(risk);
    }

    public async Task<object> UpdateAsync(Guid riskId, object request, CancellationToken ct = default)
    {
        var req = (UpdateRiskRequest)request;

        var risk = await _riskRepo.GetByIdAsync(riskId, ct)
            ?? throw new RiskNotFoundException(riskId);

        if (req.Severity != null)
        {
            if (!ValidSeverities.Contains(req.Severity))
                throw new InvalidRiskSeverityException(req.Severity);
            risk.Severity = req.Severity;
        }

        if (req.Likelihood != null)
        {
            if (!ValidLikelihoods.Contains(req.Likelihood))
                throw new InvalidRiskLikelihoodException(req.Likelihood);
            risk.Likelihood = req.Likelihood;
        }

        if (req.MitigationStatus != null)
        {
            if (!ValidMitigationStatuses.Contains(req.MitigationStatus))
                throw new InvalidMitigationStatusException(req.MitigationStatus);
            risk.MitigationStatus = req.MitigationStatus;
        }

        if (req.Title != null) risk.Title = req.Title;
        if (req.Description != null) risk.Description = req.Description;

        risk.DateUpdated = DateTime.UtcNow;
        await _riskRepo.UpdateAsync(risk, ct);

        return MapToResponse(risk);
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid riskId, CancellationToken ct = default)
    {
        var risk = await _riskRepo.GetByIdAsync(riskId, ct)
            ?? throw new RiskNotFoundException(riskId);

        risk.FlgStatus = "D";
        risk.DateUpdated = DateTime.UtcNow;
        await _riskRepo.UpdateAsync(risk, ct);
    }

    public async Task<object> ListAsync(Guid orgId, Guid projectId, Guid? sprintId,
        string? severity, string? mitigationStatus,
        int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await _riskRepo.ListAsync(
            orgId, projectId, sprintId, severity, mitigationStatus, page, pageSize, ct);

        var responses = items.Select(MapToResponse).ToList();

        return new PaginatedResponse<RiskRegisterResponse>
        {
            Data = responses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    private static void ValidateEnums(string severity, string likelihood, string mitigationStatus)
    {
        if (!ValidSeverities.Contains(severity))
            throw new InvalidRiskSeverityException(severity);
        if (!ValidLikelihoods.Contains(likelihood))
            throw new InvalidRiskLikelihoodException(likelihood);
        if (!ValidMitigationStatuses.Contains(mitigationStatus))
            throw new InvalidMitigationStatusException(mitigationStatus);
    }

    private static RiskRegisterResponse MapToResponse(RiskRegister risk)
    {
        return new RiskRegisterResponse
        {
            RiskRegisterId = risk.RiskRegisterId,
            OrganizationId = risk.OrganizationId,
            ProjectId = risk.ProjectId,
            SprintId = risk.SprintId,
            Title = risk.Title,
            Description = risk.Description,
            Severity = risk.Severity,
            Likelihood = risk.Likelihood,
            MitigationStatus = risk.MitigationStatus,
            CreatedBy = risk.CreatedBy,
            FlgStatus = risk.FlgStatus,
            DateCreated = risk.DateCreated,
            DateUpdated = risk.DateUpdated
        };
    }
}
