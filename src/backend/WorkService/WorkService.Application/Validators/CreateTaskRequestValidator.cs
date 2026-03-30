using FluentValidation;
using WorkService.Application.DTOs.Tasks;

namespace WorkService.Application.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    private static readonly HashSet<string> ValidTaskTypes = new() { "Development", "Testing", "DevOps", "Design", "Documentation", "Bug" };
    private static readonly HashSet<string> ValidPriorities = new() { "Critical", "High", "Medium", "Low" };

    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.StoryId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(3000).When(x => x.Description != null);
        RuleFor(x => x.TaskType).NotEmpty().Must(v => ValidTaskTypes.Contains(v))
            .WithMessage("TaskType must be one of: Development, Testing, DevOps, Design, Documentation, Bug.");
        RuleFor(x => x.Priority).Must(v => ValidPriorities.Contains(v));
        RuleFor(x => x.EstimatedHours).GreaterThan(0).When(x => x.EstimatedHours.HasValue);
    }
}
