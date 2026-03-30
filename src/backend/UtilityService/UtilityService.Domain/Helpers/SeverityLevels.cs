namespace UtilityService.Domain.Helpers;

public static class SeverityLevels
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Critical = "Critical";

    public static readonly string[] All = { Info, Warning, Error, Critical };
}
