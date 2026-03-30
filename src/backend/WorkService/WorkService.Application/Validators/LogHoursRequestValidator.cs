using FluentValidation;
using WorkService.Application.DTOs.Tasks;

namespace WorkService.Application.Validators;

public class LogHoursRequestValidator : AbstractValidator<LogHoursRequest>
{
    public LogHoursRequestValidator()
    {
        RuleFor(x => x.Hours).GreaterThan(0).WithMessage("Hours must be positive.");
    }
}
