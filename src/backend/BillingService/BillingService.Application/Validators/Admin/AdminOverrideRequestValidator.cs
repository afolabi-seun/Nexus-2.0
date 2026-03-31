using BillingService.Application.DTOs.Admin;
using FluentValidation;

namespace BillingService.Application.Validators.Admin;

public class AdminOverrideRequestValidator : AbstractValidator<AdminOverrideRequest>
{
    public AdminOverrideRequestValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");
    }
}
