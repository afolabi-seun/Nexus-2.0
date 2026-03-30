using FluentValidation;
using WorkService.Application.DTOs.Labels;

namespace WorkService.Application.Validators;

public class UpdateLabelRequestValidator : AbstractValidator<UpdateLabelRequest>
{
    public UpdateLabelRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(50).When(x => x.Name != null);
        RuleFor(x => x.Color).MaximumLength(7).Matches(@"^#[0-9A-Fa-f]{6}$")
            .When(x => x.Color != null);
    }
}
