namespace BillingService.Domain.Entities;

public class Plan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();
    public string PlanName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public int TierLevel { get; set; }
    public int MaxTeamMembers { get; set; }
    public int MaxDepartments { get; set; }
    public int MaxStoriesPerMonth { get; set; }
    public string? FeaturesJson { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
