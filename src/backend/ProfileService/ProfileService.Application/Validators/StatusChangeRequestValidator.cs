using FluentValidation;
using ProfileService.Application.DTOs.Organizations;

namespace ProfileService.Application.Validators;

public class StatusChangeRequestValidator : AbstractValidator<StatusChangeRequest>
{
    public StatusChangeRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty()
            .Must(v => v is "A" or "S" or "D")
            .WithMessage("Status must be one of: A (Active), S (Suspended), D (Deactivated).");
    }
}
