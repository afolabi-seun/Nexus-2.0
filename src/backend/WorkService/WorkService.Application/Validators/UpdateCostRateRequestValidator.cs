using FluentValidation;
using WorkService.Application.DTOs.CostRates;

namespace WorkService.Application.Validators;

public class UpdateCostRateRequestValidator : AbstractValidator<UpdateCostRateRequest>
{
    public UpdateCostRateRequestValidator()
    {
        RuleFor(x => x.HourlyRate).GreaterThan(0).WithMessage("Hourly rate must be positive.");
    }
}
