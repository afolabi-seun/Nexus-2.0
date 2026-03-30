using FluentValidation;
using ProfileService.Application.DTOs.TeamMembers;

namespace ProfileService.Application.Validators;

public class AvailabilityRequestValidator : AbstractValidator<AvailabilityRequest>
{
    public AvailabilityRequestValidator()
    {
        RuleFor(x => x.Availability).NotEmpty()
            .Must(v => v is "Available" or "Busy" or "Away" or "Offline")
            .WithMessage("Availability must be one of: Available, Busy, Away, Offline.");
    }
}
