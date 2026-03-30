using FluentValidation;
using WorkService.Application.DTOs.Sprints;

namespace WorkService.Application.Validators;

public class CreateSprintRequestValidator : AbstractValidator<CreateSprintRequest>
{
    public CreateSprintRequestValidator()
    {
        RuleFor(x => x.SprintName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Goal).MaximumLength(500).When(x => x.Goal != null);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty().GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
    }
}
