using FluentValidation;
using WorkService.Application.DTOs.TimeEntries;

namespace WorkService.Application.Validators;

public class CreateTimeEntryRequestValidator : AbstractValidator<CreateTimeEntryRequest>
{
    public CreateTimeEntryRequestValidator()
    {
        RuleFor(x => x.DurationMinutes).GreaterThan(0).WithMessage("Duration must be positive.");
        RuleFor(x => x.StoryId).NotEmpty();
        RuleFor(x => x.Date).LessThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Date cannot be in the future.");
    }
}
