namespace SecurityService.Domain.Exceptions;

public static class ErrorCodes
{
    // Shared
    public const string ValidationError = "VALIDATION_ERROR";
    public const int ValidationErrorValue = 1000;

    // Authentication (2001–2003)
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const int InvalidCredentialsValue = 2001;
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const int AccountLockedValue = 2002;
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const int AccountInactiveValue = 2003;

    // Password (2004–2006, 2018)
    public const string PasswordReuseNotAllowed = "PASSWORD_REUSE_NOT_ALLOWED";
    public const int PasswordReuseNotAllowedValue = 2004;
    public const string PasswordRecentlyUsed = "PASSWORD_RECENTLY_USED";
    public const int PasswordRecentlyUsedValue = 2005;
    public const string FirstTimeUserRestricted = "FIRST_TIME_USER_RESTRICTED";
    public const int FirstTimeUserRestrictedValue = 2006;
    public const string PasswordComplexityFailed = "PASSWORD_COMPLEXITY_FAILED";
    public const int PasswordComplexityFailedValue = 2018;

    // OTP (2007–2009)
    public const string OtpExpired = "OTP_EXPIRED";
    public const int OtpExpiredValue = 2007;
    public const string OtpVerificationFailed = "OTP_VERIFICATION_FAILED";
    public const int OtpVerificationFailedValue = 2008;
    public const string OtpMaxAttempts = "OTP_MAX_ATTEMPTS";
    public const int OtpMaxAttemptsValue = 2009;

    // Rate Limiting (2010)
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const int RateLimitExceededValue = 2010;

    // Authorization (2011, 2020, 2025)
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
    public const int InsufficientPermissionsValue = 2011;
    public const string DepartmentAccessDenied = "DEPARTMENT_ACCESS_DENIED";
    public const int DepartmentAccessDeniedValue = 2020;
    public const string InvalidDepartmentRole = "INVALID_DEPARTMENT_ROLE";
    public const int InvalidDepartmentRoleValue = 2025;

    // Token (2012–2013, 2024)
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const int TokenRevokedValue = 2012;
    public const string RefreshTokenReuse = "REFRESH_TOKEN_REUSE";
    public const int RefreshTokenReuseValue = 2013;
    public const string SessionExpired = "SESSION_EXPIRED";
    public const int SessionExpiredValue = 2024;

    // Service Auth (2016)
    public const string ServiceNotAuthorized = "SERVICE_NOT_AUTHORIZED";
    public const int ServiceNotAuthorizedValue = 2016;

    // Anomaly (2017)
    public const string SuspiciousLogin = "SUSPICIOUS_LOGIN";
    public const int SuspiciousLoginValue = 2017;

    // Organization (2019)
    public const string OrganizationMismatch = "ORGANIZATION_MISMATCH";
    public const int OrganizationMismatchValue = 2019;

    // General (2021–2023)
    public const string NotFound = "NOT_FOUND";
    public const int NotFoundValue = 2021;
    public const string Conflict = "CONFLICT";
    public const int ConflictValue = 2022;
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const int ServiceUnavailableValue = 2023;

    // Database Constraints
    public const string UniqueConstraintViolation = "UNIQUE_CONSTRAINT_VIOLATION";
    public const int UniqueConstraintViolationValue = 9001;
    public const string ForeignKeyViolation = "FOREIGN_KEY_VIOLATION";
    public const int ForeignKeyViolationValue = 9002;

    // Internal
    public const string InternalError = "INTERNAL_ERROR";
    public const int InternalErrorValue = 9999;
}
