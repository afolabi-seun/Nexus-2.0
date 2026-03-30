namespace BillingService.Application.DTOs.FeatureGates;

public record FeatureGateResponse(bool Allowed, long CurrentUsage, int Limit, string Feature);
