using FluentValidation;
using SecurityService.Application.DTOs.Password;

namespace SecurityService.Application.Validators;

public class PasswordResetRequestValidator : AbstractValidator<PasswordResetRequest>
{
    public PasswordResetRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
