using System.Text.Json;
using BillingService.Application.DTOs;
using BillingService.Application.DTOs.Admin;
using BillingService.Domain.Entities;
using BillingService.Domain.Exceptions;
using BillingService.Domain.Interfaces.Repositories.Plans;
using BillingService.Domain.Interfaces.Services.AdminBilling;
using BillingService.Domain.Interfaces.Services.Outbox;
using BillingService.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace BillingService.Infrastructure.Services.AdminBilling;

public class AdminPlanService : IAdminPlanService
{
    private readonly BillingDbContext _dbContext;
    private readonly IPlanRepository _planRepo;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<AdminPlanService> _logger;

    public AdminPlanService(
        BillingDbContext dbContext,
        IPlanRepository planRepo,
        IOutboxService outboxService,
        ILogger<AdminPlanService> logger)
    {
        _dbContext = dbContext;
        _planRepo = planRepo;
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task<object> GetAllPlansAsync(CancellationToken ct)
    {
        var plans = await _planRepo.GetAllAsync(ct);

        return plans.Select(p => new AdminPlanResponse(
            p.PlanId,
            p.PlanName,
            p.PlanCode,
            p.TierLevel,
            p.MaxTeamMembers,
            p.MaxDepartments,
            p.MaxStoriesPerMonth,
            p.FeaturesJson,
            p.PriceMonthly,
            p.PriceYearly,
            p.IsActive,
            p.DateCreated
        )).ToList();
    }

    public async Task<object> CreatePlanAsync(object request, CancellationToken ct)
    {
        var createRequest = (AdminCreatePlanRequest)request;

        if (await _planRepo.ExistsByCodeAsync(createRequest.PlanCode, ct))
            throw new PlanAlreadyExistsException();

        var plan = new Plan
        {
            PlanName = createRequest.PlanName,
            PlanCode = createRequest.PlanCode,
            TierLevel = createRequest.TierLevel,
            MaxTeamMembers = createRequest.MaxTeamMembers,
            MaxDepartments = createRequest.MaxDepartments,
            MaxStoriesPerMonth = createRequest.MaxStoriesPerMonth,
            PriceMonthly = createRequest.PriceMonthly,
            PriceYearly = createRequest.PriceYearly,
            FeaturesJson = createRequest.FeaturesJson,
            IsActive = true,
            DateCreated = DateTime.UtcNow
        };

        await _planRepo.AddAsync(plan, ct);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Plan {PlanCode} created with ID {PlanId}", plan.PlanCode, plan.PlanId);

        return new AdminPlanResponse(
            plan.PlanId,
            plan.PlanName,
            plan.PlanCode,
            plan.TierLevel,
            plan.MaxTeamMembers,
            plan.MaxDepartments,
            plan.MaxStoriesPerMonth,
            plan.FeaturesJson,
            plan.PriceMonthly,
            plan.PriceYearly,
            plan.IsActive,
            plan.DateCreated
        );
    }

    public async Task<object> UpdatePlanAsync(Guid planId, object request, CancellationToken ct)
    {
        var updateRequest = (AdminUpdatePlanRequest)request;

        var plan = await _planRepo.GetByIdAsync(planId, ct)
            ?? throw new PlanNotFoundException();

        plan.PlanName = updateRequest.PlanName;
        plan.TierLevel = updateRequest.TierLevel;
        plan.MaxTeamMembers = updateRequest.MaxTeamMembers;
        plan.MaxDepartments = updateRequest.MaxDepartments;
        plan.MaxStoriesPerMonth = updateRequest.MaxStoriesPerMonth;
        plan.PriceMonthly = updateRequest.PriceMonthly;
        plan.PriceYearly = updateRequest.PriceYearly;
        plan.FeaturesJson = updateRequest.FeaturesJson;

        await _planRepo.UpdateAsync(plan, ct);
        await _dbContext.SaveChangesAsync(ct);

        var newValueJson = JsonSerializer.Serialize(new
        {
            planName = updateRequest.PlanName,
            tierLevel = updateRequest.TierLevel,
            maxTeamMembers = updateRequest.MaxTeamMembers,
            maxDepartments = updateRequest.MaxDepartments,
            maxStoriesPerMonth = updateRequest.MaxStoriesPerMonth,
            priceMonthly = updateRequest.PriceMonthly,
            priceYearly = updateRequest.PriceYearly,
            featuresJson = updateRequest.FeaturesJson
        });

        await _outboxService.PublishAsync(new OutboxMessage
        {
            MessageType = "AuditEvent",
            Action = "PlanUpdated",
            EntityType = "Plan",
            EntityId = plan.PlanId.ToString(),
            OldValue = JsonSerializer.Serialize(new { planId, planCode = plan.PlanCode }),
            NewValue = newValueJson,
        }, ct);

        _logger.LogInformation("Plan {PlanId} updated", planId);

        return new AdminPlanResponse(
            plan.PlanId,
            plan.PlanName,
            plan.PlanCode,
            plan.TierLevel,
            plan.MaxTeamMembers,
            plan.MaxDepartments,
            plan.MaxStoriesPerMonth,
            plan.FeaturesJson,
            plan.PriceMonthly,
            plan.PriceYearly,
            plan.IsActive,
            plan.DateCreated
        );
    }

    public async Task<object> DeactivatePlanAsync(Guid planId, Guid adminId, CancellationToken ct)
    {
        var plan = await _planRepo.GetByIdAsync(planId, ct)
            ?? throw new PlanNotFoundException();

        plan.IsActive = false;

        await _planRepo.UpdateAsync(plan, ct);
        await _dbContext.SaveChangesAsync(ct);

        await _outboxService.PublishAsync(new OutboxMessage
        {
            MessageType = "AuditEvent",
            Action = "PlanDeactivated",
            EntityType = "Plan",
            EntityId = plan.PlanId.ToString(),
            OldValue = JsonSerializer.Serialize(new { isActive = true }),
            NewValue = JsonSerializer.Serialize(new { isActive = false, adminId }),
        }, ct);

        _logger.LogInformation("Plan {PlanId} deactivated by admin {AdminId}", planId, adminId);

        return new AdminPlanResponse(
            plan.PlanId,
            plan.PlanName,
            plan.PlanCode,
            plan.TierLevel,
            plan.MaxTeamMembers,
            plan.MaxDepartments,
            plan.MaxStoriesPerMonth,
            plan.FeaturesJson,
            plan.PriceMonthly,
            plan.PriceYearly,
            plan.IsActive,
            plan.DateCreated
        );
    }
}
