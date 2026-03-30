using BillingService.Application.DTOs.Usage;
using BillingService.Domain.Enums;
using FluentValidation;

namespace BillingService.Application.Validators;

public class IncrementUsageRequestValidator : AbstractValidator<IncrementUsageRequest>
{
    public IncrementUsageRequestValidator()
    {
        RuleFor(x => x.MetricName)
            .NotEmpty().WithMessage("MetricName is required.")
            .Must(MetricName.IsValid).WithMessage("MetricName must be one of: active_members, stories_created, storage_bytes.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Value must be a positive integer.");
    }
}
