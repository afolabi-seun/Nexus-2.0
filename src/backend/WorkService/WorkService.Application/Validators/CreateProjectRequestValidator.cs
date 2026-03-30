using FluentValidation;
using WorkService.Application.DTOs.Projects;

namespace WorkService.Application.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.ProjectName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ProjectKey).NotEmpty().Matches(@"^[A-Z0-9]{2,10}$")
            .WithMessage("ProjectKey must be 2–10 uppercase alphanumeric characters.");
    }
}
