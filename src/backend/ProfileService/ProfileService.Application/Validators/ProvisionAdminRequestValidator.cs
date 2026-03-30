using FluentValidation;
using ProfileService.Application.DTOs.Organizations;

namespace ProfileService.Application.Validators;

public class ProvisionAdminRequestValidator : AbstractValidator<ProvisionAdminRequest>
{
    public ProvisionAdminRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}
