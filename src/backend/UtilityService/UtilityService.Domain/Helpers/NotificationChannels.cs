namespace UtilityService.Domain.Helpers;

public static class NotificationChannels
{
    public const string Email = "Email";
    public const string Push = "Push";
    public const string InApp = "InApp";

    public static readonly string[] All = { Email, Push, InApp };
}
