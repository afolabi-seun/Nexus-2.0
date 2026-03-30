using FluentValidation;
using SecurityService.Application.DTOs.Auth;

namespace SecurityService.Application.Validators;

public class CredentialGenerateRequestValidator : AbstractValidator<CredentialGenerateRequest>
{
    public CredentialGenerateRequestValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
