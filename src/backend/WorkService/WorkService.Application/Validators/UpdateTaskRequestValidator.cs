using FluentValidation;
using WorkService.Application.DTOs.Tasks;

namespace WorkService.Application.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    private static readonly HashSet<string> ValidPriorities = new() { "Critical", "High", "Medium", "Low" };

    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title != null);
        RuleFor(x => x.Description).MaximumLength(3000).When(x => x.Description != null);
        RuleFor(x => x.Priority).Must(v => ValidPriorities.Contains(v!))
            .When(x => x.Priority != null);
        RuleFor(x => x.EstimatedHours).GreaterThan(0).When(x => x.EstimatedHours.HasValue);
    }
}
