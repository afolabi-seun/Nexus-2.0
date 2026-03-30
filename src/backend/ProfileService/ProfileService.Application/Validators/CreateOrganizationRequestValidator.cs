using FluentValidation;
using ProfileService.Application.DTOs.Organizations;

namespace ProfileService.Application.Validators;

public class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StoryIdPrefix).NotEmpty().Matches(@"^[A-Z0-9]{2,10}$")
            .WithMessage("StoryIdPrefix must be 2–10 uppercase alphanumeric characters.");
        RuleFor(x => x.TimeZone).NotEmpty();
        RuleFor(x => x.DefaultSprintDurationWeeks).InclusiveBetween(1, 4);
    }
}
