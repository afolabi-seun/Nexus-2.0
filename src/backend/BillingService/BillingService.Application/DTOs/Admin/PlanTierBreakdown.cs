namespace BillingService.Application.DTOs.Admin;

public record PlanTierBreakdown(
    string PlanName,
    string PlanCode,
    int OrganizationCount);
