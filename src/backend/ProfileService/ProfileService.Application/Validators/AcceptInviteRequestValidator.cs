using FluentValidation;
using ProfileService.Application.DTOs.Invites;

namespace ProfileService.Application.Validators;

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.OtpCode).NotEmpty().Length(6).Matches(@"^\d{6}$");
    }
}
