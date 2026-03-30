using FluentValidation;
using SecurityService.Application.DTOs.Password;

namespace SecurityService.Application.Validators;

public class ForcedPasswordChangeRequestValidator : AbstractValidator<ForcedPasswordChangeRequest>
{
    public ForcedPasswordChangeRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Must contain at least one digit.")
            .Matches(@"[!@#\$%\^&\*]").WithMessage("Must contain at least one special character (!@#$%^&*).");
    }
}
