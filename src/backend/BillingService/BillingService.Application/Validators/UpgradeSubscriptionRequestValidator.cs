using BillingService.Application.DTOs.Subscriptions;
using FluentValidation;

namespace BillingService.Application.Validators;

public class UpgradeSubscriptionRequestValidator : AbstractValidator<UpgradeSubscriptionRequest>
{
    public UpgradeSubscriptionRequestValidator()
    {
        RuleFor(x => x.NewPlanId)
            .NotEmpty().WithMessage("NewPlanId is required.");
    }
}
