using FluentValidation;
using WorkService.Application.DTOs.Labels;

namespace WorkService.Application.Validators;

public class CreateLabelRequestValidator : AbstractValidator<CreateLabelRequest>
{
    public CreateLabelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(7).Matches(@"^#[0-9A-Fa-f]{6}$");
    }
}
