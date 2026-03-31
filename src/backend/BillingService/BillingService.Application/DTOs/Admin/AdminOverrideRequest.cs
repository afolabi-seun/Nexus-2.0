namespace BillingService.Application.DTOs.Admin;

public record AdminOverrideRequest(
    Guid PlanId,
    string? Reason);
