using FluentValidation;
using SecurityService.Application.DTOs.Otp;

namespace SecurityService.Application.Validators;

public class OtpVerifyRequestValidator : AbstractValidator<OtpVerifyRequest>
{
    public OtpVerifyRequestValidator()
    {
        RuleFor(x => x.Identity).NotEmpty();
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches(@"^\d{6}$").WithMessage("Code must be exactly 6 digits.");
    }
}
