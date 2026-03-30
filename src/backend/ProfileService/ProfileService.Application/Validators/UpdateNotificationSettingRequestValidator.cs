using FluentValidation;
using ProfileService.Application.DTOs.NotificationSettings;

namespace ProfileService.Application.Validators;

public class UpdateNotificationSettingRequestValidator : AbstractValidator<UpdateNotificationSettingRequest>
{
    public UpdateNotificationSettingRequestValidator()
    {
        // All booleans — no additional validation needed beyond model binding
    }
}
