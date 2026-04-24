namespace WorkService.Domain.Exceptions;

public static class ErrorCodes
{
    // Shared
    public const string ValidationError = "VALIDATION_ERROR";
    public const int ValidationErrorValue = 1000;

    // Story (4001, 4004, 4013–4015, 4020, 4023–4024, 4026, 4039)
    public const string StoryNotFound = "STORY_NOT_FOUND";
    public const int StoryNotFoundValue = 4001;
    public const string InvalidStoryTransition = "INVALID_STORY_TRANSITION";
    public const int InvalidStoryTransitionValue = 4004;
    public const string StoryRequiresAssignee = "STORY_REQUIRES_ASSIGNEE";
    public const int StoryRequiresAssigneeValue = 4013;
    public const string StoryRequiresTasks = "STORY_REQUIRES_TASKS";
    public const int StoryRequiresTasksValue = 4014;
    public const string StoryRequiresPoints = "STORY_REQUIRES_POINTS";
    public const int StoryRequiresPointsValue = 4015;
    public const string StoryKeyNotFound = "STORY_KEY_NOT_FOUND";
    public const int StoryKeyNotFoundValue = 4020;
    public const string InvalidStoryPoints = "INVALID_STORY_POINTS";
    public const int InvalidStoryPointsValue = 4023;
    public const string InvalidPriority = "INVALID_PRIORITY";
    public const int InvalidPriorityValue = 4024;
    public const string StoryInActiveSprint = "STORY_IN_ACTIVE_SPRINT";
    public const int StoryInActiveSprintValue = 4026;
    public const string StoryDescriptionRequired = "STORY_DESCRIPTION_REQUIRED";
    public const int StoryDescriptionRequiredValue = 4039;

    // Task (4002, 4005, 4018–4019, 4025, 4027, 4035)
    public const string TaskNotFound = "TASK_NOT_FOUND";
    public const int TaskNotFoundValue = 4002;
    public const string InvalidTaskTransition = "INVALID_TASK_TRANSITION";
    public const int InvalidTaskTransitionValue = 4005;
    public const string AssigneeNotInDepartment = "ASSIGNEE_NOT_IN_DEPARTMENT";
    public const int AssigneeNotInDepartmentValue = 4018;
    public const string AssigneeAtCapacity = "ASSIGNEE_AT_CAPACITY";
    public const int AssigneeAtCapacityValue = 4019;
    public const string InvalidTaskType = "INVALID_TASK_TYPE";
    public const int InvalidTaskTypeValue = 4025;
    public const string TaskInProgress = "TASK_IN_PROGRESS";
    public const int TaskInProgressValue = 4027;
    public const string HoursMustBePositive = "HOURS_MUST_BE_POSITIVE";
    public const int HoursMustBePositiveValue = 4035;

    // Sprint (4003, 4006–4009, 4016, 4021–4022, 4033)
    public const string SprintNotFound = "SPRINT_NOT_FOUND";
    public const int SprintNotFoundValue = 4003;
    public const string SprintNotInPlanning = "SPRINT_NOT_IN_PLANNING";
    public const int SprintNotInPlanningValue = 4006;
    public const string StoryAlreadyInSprint = "STORY_ALREADY_IN_SPRINT";
    public const int StoryAlreadyInSprintValue = 4007;
    public const string StoryNotInSprint = "STORY_NOT_IN_SPRINT";
    public const int StoryNotInSprintValue = 4008;
    public const string SprintOverlap = "SPRINT_OVERLAP";
    public const int SprintOverlapValue = 4009;
    public const string OnlyOneActiveSprint = "ONLY_ONE_ACTIVE_SPRINT";
    public const int OnlyOneActiveSprintValue = 4016;
    public const string SprintAlreadyActive = "SPRINT_ALREADY_ACTIVE";
    public const int SprintAlreadyActiveValue = 4021;
    public const string SprintAlreadyCompleted = "SPRINT_ALREADY_COMPLETED";
    public const int SprintAlreadyCompletedValue = 4022;
    public const string SprintEndBeforeStart = "SPRINT_END_BEFORE_START";
    public const int SprintEndBeforeStartValue = 4033;

    // Label (4010–4011, 4040)
    public const string LabelNotFound = "LABEL_NOT_FOUND";
    public const int LabelNotFoundValue = 4010;
    public const string LabelNameDuplicate = "LABEL_NAME_DUPLICATE";
    public const int LabelNameDuplicateValue = 4011;
    public const string MaxLabelsPerStory = "MAX_LABELS_PER_STORY";
    public const int MaxLabelsPerStoryValue = 4040;

    // Comment (4012, 4017, 4029)
    public const string CommentNotFound = "COMMENT_NOT_FOUND";
    public const int CommentNotFoundValue = 4012;
    public const string CommentNotAuthor = "COMMENT_NOT_AUTHOR";
    public const int CommentNotAuthorValue = 4017;
    public const string MentionUserNotFound = "MENTION_USER_NOT_FOUND";
    public const int MentionUserNotFoundValue = 4029;

    // Search (4028)
    public const string SearchQueryTooShort = "SEARCH_QUERY_TOO_SHORT";
    public const int SearchQueryTooShortValue = 4028;

    // Sequence (4034)
    public const string StorySequenceInitFailed = "STORY_SEQUENCE_INIT_FAILED";
    public const int StorySequenceInitFailedValue = 4034;

    // Authorization (4030–4032, 4070–4072)
    public const string OrganizationMismatch = "ORGANIZATION_MISMATCH";
    public const int OrganizationMismatchValue = 4030;
    public const string DepartmentAccessDenied = "DEPARTMENT_ACCESS_DENIED";
    public const int DepartmentAccessDeniedValue = 4031;
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
    public const int InsufficientPermissionsValue = 4032;
    public const string OrgAdminRequired = "ORGADMIN_REQUIRED";
    public const int OrgAdminRequiredValue = 4070;
    public const string DeptLeadRequired = "DEPTLEAD_REQUIRED";
    public const int DeptLeadRequiredValue = 4071;
    public const string PlatformAdminRequired = "PLATFORM_ADMIN_REQUIRED";
    public const int PlatformAdminRequiredValue = 4072;

    // General (4036–4038)
    public const string NotFound = "NOT_FOUND";
    public const int NotFoundValue = 4036;
    public const string Conflict = "CONFLICT";
    public const int ConflictValue = 4037;
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const int ServiceUnavailableValue = 4038;

    // Project (4041–4046)
    public const string ProjectNotFound = "PROJECT_NOT_FOUND";
    public const int ProjectNotFoundValue = 4041;
    public const string ProjectNameDuplicate = "PROJECT_NAME_DUPLICATE";
    public const int ProjectNameDuplicateValue = 4042;
    public const string ProjectKeyDuplicate = "PROJECT_KEY_DUPLICATE";
    public const int ProjectKeyDuplicateValue = 4043;
    public const string ProjectKeyImmutable = "PROJECT_KEY_IMMUTABLE";
    public const int ProjectKeyImmutableValue = 4044;
    public const string ProjectKeyInvalidFormat = "PROJECT_KEY_INVALID_FORMAT";
    public const int ProjectKeyInvalidFormatValue = 4045;
    public const string StoryProjectMismatch = "STORY_PROJECT_MISMATCH";
    public const int StoryProjectMismatchValue = 4046;

    // Time Tracking (4050–4056)
    public const string TimerAlreadyActive = "TIMER_ALREADY_ACTIVE";
    public const int TimerAlreadyActiveValue = 4050;
    public const string NoActiveTimer = "NO_ACTIVE_TIMER";
    public const int NoActiveTimerValue = 4051;
    public const string TimeEntryNotFound = "TIME_ENTRY_NOT_FOUND";
    public const int TimeEntryNotFoundValue = 4052;
    public const string CostRateDuplicate = "COST_RATE_DUPLICATE";
    public const int CostRateDuplicateValue = 4053;
    public const string InvalidCostRate = "INVALID_COST_RATE";
    public const int InvalidCostRateValue = 4054;
    public const string InvalidTimePolicy = "INVALID_TIME_POLICY";
    public const int InvalidTimePolicyValue = 4055;
    public const string DailyHoursExceeded = "DAILY_HOURS_EXCEEDED";
    public const int DailyHoursExceededValue = 4056;

    // Analytics & Reporting (4060–4065)
    public const string InvalidAnalyticsParameter = "INVALID_ANALYTICS_PARAMETER";
    public const int InvalidAnalyticsParameterValue = 4060;
    public const string InvalidRiskSeverity = "INVALID_RISK_SEVERITY";
    public const int InvalidRiskSeverityValue = 4061;
    public const string InvalidRiskLikelihood = "INVALID_RISK_LIKELIHOOD";
    public const int InvalidRiskLikelihoodValue = 4062;
    public const string InvalidMitigationStatus = "INVALID_MITIGATION_STATUS";
    public const int InvalidMitigationStatusValue = 4063;
    public const string RiskNotFound = "RISK_NOT_FOUND";
    public const int RiskNotFoundValue = 4064;
    public const string SnapshotGenerationFailed = "SNAPSHOT_GENERATION_FAILED";
    public const int SnapshotGenerationFailedValue = 4065;

    public const string InvalidStoryType = "INVALID_STORY_TYPE";
    public const int InvalidStoryTypeValue = 4066;

    // Database Constraints
    public const string UniqueConstraintViolation = "UNIQUE_CONSTRAINT_VIOLATION";
    public const int UniqueConstraintViolationValue = 9001;
    public const string ForeignKeyViolation = "FOREIGN_KEY_VIOLATION";
    public const int ForeignKeyViolationValue = 9002;


    // Authentication (JWT)
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const int TokenExpiredValue = 9003;
    public const string InvalidToken = "INVALID_TOKEN";
    public const int InvalidTokenValue = 9004;

    // Internal
    public const string InternalError = "INTERNAL_ERROR";
    public const int InternalErrorValue = 9999;
}
