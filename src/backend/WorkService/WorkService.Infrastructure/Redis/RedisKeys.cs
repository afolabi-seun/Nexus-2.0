namespace WorkService.Infrastructure.Redis;

public static class RedisKeys
{
    private const string P = "nexus:";

    // Sprints
    public static string SprintActive(Guid projectId) => $"{P}sprint_active:{projectId}";
    public static string SprintMetrics(Guid sprintId) => $"{P}sprint_metrics:{sprintId}";
    public static string SprintNotif(Guid sprintId, string date) => $"{P}sprint_notif:{sprintId}:{date}";

    // Boards
    public static string BoardKanban(Guid organizationId, Guid? projectId, Guid? sprintId) => $"{P}board_kanban:{organizationId}:{projectId}:{sprintId}";
    public static string BoardBacklog(Guid organizationId, Guid? projectId) => $"{P}board_backlog:{organizationId}:{projectId}";
    public static string BoardDept(Guid organizationId, Guid? projectId, Guid? sprintId) => $"{P}board_dept:{organizationId}:{projectId}:{sprintId}";

    // Analytics
    public static string AnalyticsDashboard(Guid projectId) => $"{P}analytics:dashboard:{projectId}";
    public const string AnalyticsSnapshotStatus = $"{P}analytics:snapshot_status";

    // Search
    public static string SearchResults(string hash) => $"{P}search_results:{hash}";

    // Stories
    public static string ProjectPrefix(Guid projectId) => $"{P}project_prefix:{projectId}";

    // Timer
    public static string Timer(Guid userId, Guid storyId) => $"{P}timer:{userId}:{storyId}";
    public static string TimerPattern(Guid userId) => $"{P}timer:{userId}:*";

    // Workflows
    public static string WorkflowOrg(Guid organizationId) => $"{P}workflow_override:org:{organizationId}";
    public static string WorkflowDept(Guid organizationId, Guid departmentId) => $"{P}workflow_override:dept:{organizationId}:{departmentId}";

    // Auth
    public static string Blacklist(string jti) => $"{P}blacklist:{jti}";
    public static string ErrorCode(string errorCode) => $"{P}error_code:{errorCode}";

    // Outbox
    public const string Outbox = $"{P}outbox:work";
    public const string Dlq = $"{P}dlq:work";
}
