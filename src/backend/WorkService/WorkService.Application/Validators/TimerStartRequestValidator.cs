using FluentValidation;
using WorkService.Application.DTOs.TimeEntries;

namespace WorkService.Application.Validators;

public class TimerStartRequestValidator : AbstractValidator<TimerStartRequest>
{
    public TimerStartRequestValidator()
    {
        RuleFor(x => x.StoryId).NotEmpty();
    }
}
