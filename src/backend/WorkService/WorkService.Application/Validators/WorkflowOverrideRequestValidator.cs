using FluentValidation;
using WorkService.Application.DTOs.Workflows;

namespace WorkService.Application.Validators;

public class WorkflowOverrideRequestValidator : AbstractValidator<WorkflowOverrideRequest>
{
    public WorkflowOverrideRequestValidator()
    {
        RuleFor(x => x).Must(x =>
            x.StoryTransitions != null || x.TaskTransitions != null || x.CustomStatuses != null)
            .WithMessage("At least one override (StoryTransitions, TaskTransitions, or CustomStatuses) must be provided.");
    }
}
