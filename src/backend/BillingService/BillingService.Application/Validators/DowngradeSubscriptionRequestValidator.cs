using BillingService.Application.DTOs.Subscriptions;
using FluentValidation;

namespace BillingService.Application.Validators;

public class DowngradeSubscriptionRequestValidator : AbstractValidator<DowngradeSubscriptionRequest>
{
    public DowngradeSubscriptionRequestValidator()
    {
        RuleFor(x => x.NewPlanId)
            .NotEmpty().WithMessage("NewPlanId is required.");
    }
}
