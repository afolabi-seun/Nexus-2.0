using FluentValidation;
using WorkService.Application.DTOs.Sprints;

namespace WorkService.Application.Validators;

public class UpdateSprintRequestValidator : AbstractValidator<UpdateSprintRequest>
{
    public UpdateSprintRequestValidator()
    {
        RuleFor(x => x.SprintName).MaximumLength(100).When(x => x.SprintName != null);
        RuleFor(x => x.Goal).MaximumLength(500).When(x => x.Goal != null);
    }
}
