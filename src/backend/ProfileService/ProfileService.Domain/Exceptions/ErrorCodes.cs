namespace ProfileService.Domain.Exceptions;

public static class ErrorCodes
{
    // Shared
    public const string ValidationError = "VALIDATION_ERROR";
    public const int ValidationErrorValue = 1000;

    // Profile (3001–3027)
    public const string EmailAlreadyRegistered = "EMAIL_ALREADY_REGISTERED";
    public const int EmailAlreadyRegisteredValue = 3001;

    public const string InviteExpiredOrInvalid = "INVITE_EXPIRED_OR_INVALID";
    public const int InviteExpiredOrInvalidValue = 3002;

    public const string MaxDevicesReached = "MAX_DEVICES_REACHED";
    public const int MaxDevicesReachedValue = 3003;

    public const string LastOrgAdminCannotDeactivate = "LAST_ORGADMIN_CANNOT_DEACTIVATE";
    public const int LastOrgAdminCannotDeactivateValue = 3004;

    public const string OrganizationNameDuplicate = "ORGANIZATION_NAME_DUPLICATE";
    public const int OrganizationNameDuplicateValue = 3005;

    public const string StoryPrefixDuplicate = "STORY_PREFIX_DUPLICATE";
    public const int StoryPrefixDuplicateValue = 3006;

    public const string StoryPrefixImmutable = "STORY_PREFIX_IMMUTABLE";
    public const int StoryPrefixImmutableValue = 3007;

    public const string DepartmentNameDuplicate = "DEPARTMENT_NAME_DUPLICATE";
    public const int DepartmentNameDuplicateValue = 3008;

    public const string DepartmentCodeDuplicate = "DEPARTMENT_CODE_DUPLICATE";
    public const int DepartmentCodeDuplicateValue = 3009;

    public const string DefaultDepartmentCannotDelete = "DEFAULT_DEPARTMENT_CANNOT_DELETE";
    public const int DefaultDepartmentCannotDeleteValue = 3010;

    public const string MemberAlreadyInDepartment = "MEMBER_ALREADY_IN_DEPARTMENT";
    public const int MemberAlreadyInDepartmentValue = 3011;

    public const string MemberMustHaveDepartment = "MEMBER_MUST_HAVE_DEPARTMENT";
    public const int MemberMustHaveDepartmentValue = 3012;

    public const string InvalidRoleAssignment = "INVALID_ROLE_ASSIGNMENT";
    public const int InvalidRoleAssignmentValue = 3013;

    public const string InviteEmailAlreadyMember = "INVITE_EMAIL_ALREADY_MEMBER";
    public const int InviteEmailAlreadyMemberValue = 3014;

    public const string OrganizationMismatch = "ORGANIZATION_MISMATCH";
    public const int OrganizationMismatchValue = 3015;

    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const int RateLimitExceededValue = 3016;

    public const string DepartmentHasActiveMembers = "DEPARTMENT_HAS_ACTIVE_MEMBERS";
    public const int DepartmentHasActiveMembersValue = 3017;

    public const string MemberNotInDepartment = "MEMBER_NOT_IN_DEPARTMENT";
    public const int MemberNotInDepartmentValue = 3018;

    public const string InvalidAvailabilityStatus = "INVALID_AVAILABILITY_STATUS";
    public const int InvalidAvailabilityStatusValue = 3019;

    public const string StoryPrefixInvalidFormat = "STORY_PREFIX_INVALID_FORMAT";
    public const int StoryPrefixInvalidFormatValue = 3020;

    public const string NotFound = "NOT_FOUND";
    public const int NotFoundValue = 3021;

    public const string Conflict = "CONFLICT";
    public const int ConflictValue = 3022;

    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const int ServiceUnavailableValue = 3023;

    public const string DepartmentNotFound = "DEPARTMENT_NOT_FOUND";
    public const int DepartmentNotFoundValue = 3024;

    public const string MemberNotFound = "MEMBER_NOT_FOUND";
    public const int MemberNotFoundValue = 3025;

    public const string InvalidPreferenceValue = "INVALID_PREFERENCE_VALUE";
    public const int InvalidPreferenceValueValue = 3026;

    public const string PreferenceKeyUnknown = "PREFERENCE_KEY_UNKNOWN";
    public const int PreferenceKeyUnknownValue = 3027;

    // Role Restrictions (3028–3031)
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
    public const int InsufficientPermissionsValue = 3028;

    public const string OrgAdminRequired = "ORGADMIN_REQUIRED";
    public const int OrgAdminRequiredValue = 3029;

    public const string DeptLeadRequired = "DEPTLEAD_REQUIRED";
    public const int DeptLeadRequiredValue = 3030;

    public const string PlatformAdminRequired = "PLATFORM_ADMIN_REQUIRED";
    public const int PlatformAdminRequiredValue = 3031;

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
