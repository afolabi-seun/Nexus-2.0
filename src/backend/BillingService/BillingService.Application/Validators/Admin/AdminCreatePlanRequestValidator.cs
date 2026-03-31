using BillingService.Application.DTOs.Admin;
using FluentValidation;

namespace BillingService.Application.Validators.Admin;

public class AdminCreatePlanRequestValidator : AbstractValidator<AdminCreatePlanRequest>
{
    public AdminCreatePlanRequestValidator()
    {
        RuleFor(x => x.PlanName)
            .NotEmpty().WithMessage("PlanName is required.");

        RuleFor(x => x.PlanCode)
            .NotEmpty().WithMessage("PlanCode is required.")
            .Matches(@"^[A-Z0-9_]{2,20}$").WithMessage("PlanCode must be 2-20 characters, uppercase letters, digits, or underscores.");

        RuleFor(x => x.TierLevel)
            .GreaterThan(0).WithMessage("TierLevel must be a positive integer.");

        RuleFor(x => x.MaxTeamMembers)
            .GreaterThan(0).WithMessage("MaxTeamMembers must be a positive integer.");

        RuleFor(x => x.MaxDepartments)
            .GreaterThan(0).WithMessage("MaxDepartments must be a positive integer.");

        RuleFor(x => x.MaxStoriesPerMonth)
            .GreaterThan(0).WithMessage("MaxStoriesPerMonth must be a positive integer.");

        RuleFor(x => x.PriceMonthly)
            .GreaterThanOrEqualTo(0).WithMessage("PriceMonthly must be a non-negative decimal.");

        RuleFor(x => x.PriceYearly)
            .GreaterThanOrEqualTo(0).WithMessage("PriceYearly must be a non-negative decimal.");
    }
}
