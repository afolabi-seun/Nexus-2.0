using Microsoft.Extensions.Logging;
using WorkService.Application.DTOs.TimePolicies;
using WorkService.Domain.Entities;
using WorkService.Domain.Exceptions;
using WorkService.Domain.Interfaces.Repositories.TimePolicies;
using WorkService.Domain.Interfaces.Services.TimePolicies;

namespace WorkService.Infrastructure.Services.TimePolicies;

public class TimePolicyService : ITimePolicyService
{
    private readonly ITimePolicyRepository _policyRepo;
    private readonly ILogger<TimePolicyService> _logger;

    public TimePolicyService(ITimePolicyRepository policyRepo, ILogger<TimePolicyService> logger)
    {
        _policyRepo = policyRepo;
        _logger = logger;
    }

    public async Task<object> GetPolicyAsync(Guid orgId, CancellationToken ct = default)
    {
        var policy = await _policyRepo.GetByOrganizationAsync(orgId, ct);

        if (policy == null)
        {
            // Return default values
            return new TimePolicyResponse
            {
                TimePolicyId = Guid.Empty,
                OrganizationId = orgId,
                RequiredHoursPerDay = 8m,
                OvertimeThresholdHoursPerDay = 10m,
                ApprovalRequired = false,
                ApprovalWorkflow = "None",
                MaxDailyHours = 24m,
                FlgStatus = "A",
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            };
        }

        return MapToResponse(policy);
    }

    public async Task<object> UpsertAsync(Guid orgId, Guid userId, string userRole, object request, CancellationToken ct = default)
    {
        if (userRole != "OrgAdmin")
            throw new InsufficientPermissionsException();

        var req = (UpdateTimePolicyRequest)request;

        // Validate fields
        if (req.RequiredHoursPerDay <= 0 || req.RequiredHoursPerDay > 24)
            throw new InvalidTimePolicyException("RequiredHoursPerDay must be greater than 0 and at most 24.");

        if (req.MaxDailyHours < req.RequiredHoursPerDay)
            throw new InvalidTimePolicyException("MaxDailyHours must be greater than or equal to RequiredHoursPerDay.");

        var existing = await _policyRepo.GetByOrganizationAsync(orgId, ct);

        if (existing == null)
        {
            var policy = new TimePolicy
            {
                OrganizationId = orgId,
                RequiredHoursPerDay = req.RequiredHoursPerDay,
                OvertimeThresholdHoursPerDay = req.OvertimeThresholdHoursPerDay,
                ApprovalRequired = req.ApprovalRequired,
                ApprovalWorkflow = req.ApprovalWorkflow,
                MaxDailyHours = req.MaxDailyHours
            };

            await _policyRepo.AddAsync(policy, ct);
            return MapToResponse(policy);
        }

        existing.RequiredHoursPerDay = req.RequiredHoursPerDay;
        existing.OvertimeThresholdHoursPerDay = req.OvertimeThresholdHoursPerDay;
        existing.ApprovalRequired = req.ApprovalRequired;
        existing.ApprovalWorkflow = req.ApprovalWorkflow;
        existing.MaxDailyHours = req.MaxDailyHours;
        existing.DateUpdated = DateTime.UtcNow;

        await _policyRepo.UpdateAsync(existing, ct);
        return MapToResponse(existing);
    }

    private static TimePolicyResponse MapToResponse(TimePolicy policy)
    {
        return new TimePolicyResponse
        {
            TimePolicyId = policy.TimePolicyId,
            OrganizationId = policy.OrganizationId,
            RequiredHoursPerDay = policy.RequiredHoursPerDay,
            OvertimeThresholdHoursPerDay = policy.OvertimeThresholdHoursPerDay,
            ApprovalRequired = policy.ApprovalRequired,
            ApprovalWorkflow = policy.ApprovalWorkflow,
            MaxDailyHours = policy.MaxDailyHours,
            FlgStatus = policy.FlgStatus,
            DateCreated = policy.DateCreated,
            DateUpdated = policy.DateUpdated
        };
    }
}
