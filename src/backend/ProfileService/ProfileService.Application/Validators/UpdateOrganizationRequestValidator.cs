using FluentValidation;
using ProfileService.Application.DTOs.Organizations;

namespace ProfileService.Application.Validators;

public class UpdateOrganizationRequestValidator : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationRequestValidator()
    {
        RuleFor(x => x.OrganizationName).MaximumLength(200).When(x => x.OrganizationName != null);
        RuleFor(x => x.DefaultSprintDurationWeeks).InclusiveBetween(1, 4).When(x => x.DefaultSprintDurationWeeks.HasValue);
    }
}
