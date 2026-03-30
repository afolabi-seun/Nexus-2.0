using FluentValidation;
using UtilityService.Application.DTOs.Notifications;

namespace UtilityService.Application.Validators;

public class DispatchNotificationRequestValidator : AbstractValidator<DispatchNotificationRequest>
{
    private static readonly string[] ValidTypes =
    {
        "StoryAssigned", "TaskAssigned", "SprintStarted", "SprintEnded",
        "MentionedInComment", "StoryStatusChanged", "TaskStatusChanged", "DueDateApproaching"
    };

    private static readonly string[] ValidChannels = { "Email", "Push", "InApp" };

    public DispatchNotificationRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NotificationType).NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("NotificationType must be one of: " + string.Join(", ", ValidTypes));
        RuleFor(x => x.Channels).NotEmpty()
            .Must(c => c.Split(',').All(ch => ValidChannels.Contains(ch.Trim())))
            .WithMessage("Each channel must be one of: " + string.Join(", ", ValidChannels));
        RuleFor(x => x.Recipient).NotEmpty();
    }
}
