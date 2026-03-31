using FluentValidation;
using WorkService.Application.DTOs.TimeEntries;

namespace WorkService.Application.Validators;

public class UpdateTimeEntryRequestValidator : AbstractValidator<UpdateTimeEntryRequest>
{
    public UpdateTimeEntryRequestValidator()
    {
        RuleFor(x => x.DurationMinutes).GreaterThan(0).WithMessage("Duration must be positive.")
            .When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.Date).LessThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Date cannot be in the future.")
            .When(x => x.Date.HasValue);
    }
}
