using FluentValidation;
using WorkService.Application.DTOs.Projects;

namespace WorkService.Application.Validators;

public class ProjectStatusRequestValidator : AbstractValidator<ProjectStatusRequest>
{
    private static readonly string[] ValidStatuses = ["Active", "Completed", "Archived", "Suspended"];

    public ProjectStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .MaximumLength(20).WithMessage("Status must not exceed 20 characters.")
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
