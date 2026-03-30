using FluentValidation;
using UtilityService.Application.DTOs.ReferenceData;

namespace UtilityService.Application.Validators;

public class CreatePriorityLevelRequestValidator : AbstractValidator<CreatePriorityLevelRequest>
{
    public CreatePriorityLevelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThan(0);
        RuleFor(x => x.Color).NotEmpty().Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a valid hex color (e.g., #DC2626).");
    }
}
