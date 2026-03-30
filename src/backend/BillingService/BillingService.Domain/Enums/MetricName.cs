namespace BillingService.Domain.Enums;

public static class MetricName
{
    public const string ActiveMembers = "active_members";
    public const string StoriesCreated = "stories_created";
    public const string StorageBytes = "storage_bytes";

    public static readonly string[] All = [ActiveMembers, StoriesCreated, StorageBytes];

    public static bool IsValid(string name) =>
        name == ActiveMembers || name == StoriesCreated || name == StorageBytes;
}
