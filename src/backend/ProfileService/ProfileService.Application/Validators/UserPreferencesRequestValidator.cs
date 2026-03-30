using FluentValidation;
using ProfileService.Application.DTOs.Preferences;

namespace ProfileService.Application.Validators;

public class UserPreferencesRequestValidator : AbstractValidator<UserPreferencesRequest>
{
    public UserPreferencesRequestValidator()
    {
        RuleFor(x => x.Theme)
            .Must(v => v is "Light" or "Dark" or "System")
            .When(x => x.Theme != null);
        RuleFor(x => x.Language).MaximumLength(10).When(x => x.Language != null);
        RuleFor(x => x.DefaultBoardView)
            .Must(v => v is "Kanban" or "Sprint" or "Backlog")
            .When(x => x.DefaultBoardView != null);
        RuleFor(x => x.EmailDigestFrequency)
            .Must(v => v is "Realtime" or "Hourly" or "Daily" or "Off")
            .When(x => x.EmailDigestFrequency != null);
        RuleFor(x => x.DateFormat)
            .Must(v => v is "ISO" or "US" or "EU")
            .When(x => x.DateFormat != null);
        RuleFor(x => x.TimeFormat)
            .Must(v => v is "H24" or "H12")
            .When(x => x.TimeFormat != null);
    }
}
