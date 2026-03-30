using FluentValidation;
using ProfileService.Application.DTOs.Organizations;

namespace ProfileService.Application.Validators;

public class OrganizationSettingsRequestValidator : AbstractValidator<OrganizationSettingsRequest>
{
    public OrganizationSettingsRequestValidator()
    {
        RuleFor(x => x.StoryIdPrefix).Matches(@"^[A-Z0-9]{2,10}$")
            .When(x => x.StoryIdPrefix != null)
            .WithMessage("StoryIdPrefix must be 2–10 uppercase alphanumeric characters.");
        RuleFor(x => x.DefaultSprintDurationWeeks).InclusiveBetween(1, 4).When(x => x.DefaultSprintDurationWeeks.HasValue);
        RuleFor(x => x.AuditRetentionDays).GreaterThan(0).When(x => x.AuditRetentionDays.HasValue);
        RuleFor(x => x.DefaultWipLimit).GreaterThanOrEqualTo(0).When(x => x.DefaultWipLimit.HasValue);
        RuleFor(x => x.StoryPointScale)
            .Must(v => v is "Fibonacci" or "Linear" or "TShirt")
            .When(x => x.StoryPointScale != null);
        RuleFor(x => x.AutoAssignmentStrategy)
            .Must(v => v is "LeastLoaded" or "RoundRobin")
            .When(x => x.AutoAssignmentStrategy != null);
        RuleFor(x => x.DefaultBoardView)
            .Must(v => v is "Kanban" or "Sprint" or "Backlog")
            .When(x => x.DefaultBoardView != null);
        RuleFor(x => x.DigestFrequency)
            .Must(v => v is "Realtime" or "Hourly" or "Daily")
            .When(x => x.DigestFrequency != null);
    }
}
