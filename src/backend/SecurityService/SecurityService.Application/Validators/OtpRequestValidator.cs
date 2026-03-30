using FluentValidation;
using SecurityService.Application.DTOs.Otp;

namespace SecurityService.Application.Validators;

public class OtpRequestValidator : AbstractValidator<OtpRequest>
{
    public OtpRequestValidator()
    {
        RuleFor(x => x.Identity).NotEmpty();
    }
}
