using FluentValidation;
using WorkService.Application.DTOs.Projects;

namespace WorkService.Application.Validators;

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.ProjectName).MaximumLength(200).When(x => x.ProjectName != null);
        RuleFor(x => x.ProjectKey).Matches(@"^[A-Z0-9]{2,10}$")
            .When(x => x.ProjectKey != null)
            .WithMessage("ProjectKey must be 2–10 uppercase alphanumeric characters.");
    }
}
