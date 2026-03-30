namespace UtilityService.Domain.Exceptions;

public static class ErrorCodes
{
    // Shared
    public const string ValidationError = "VALIDATION_ERROR";
    public const int ValidationErrorValue = 1000;

    // Audit (6001)
    public const string AuditLogImmutable = "AUDIT_LOG_IMMUTABLE";
    public const int AuditLogImmutableValue = 6001;

    // Error Codes (6002–6003)
    public const string ErrorCodeDuplicate = "ERROR_CODE_DUPLICATE";
    public const int ErrorCodeDuplicateValue = 6002;
    public const string ErrorCodeNotFound = "ERROR_CODE_NOT_FOUND";
    public const int ErrorCodeNotFoundValue = 6003;

    // Notifications (6004, 6011–6012)
    public const string NotificationDispatchFailed = "NOTIFICATION_DISPATCH_FAILED";
    public const int NotificationDispatchFailedValue = 6004;
    public const string InvalidNotificationType = "INVALID_NOTIFICATION_TYPE";
    public const int InvalidNotificationTypeValue = 6011;
    public const string InvalidChannel = "INVALID_CHANNEL";
    public const int InvalidChannelValue = 6012;

    // Reference Data (6005, 6014)
    public const string ReferenceDataNotFound = "REFERENCE_DATA_NOT_FOUND";
    public const int ReferenceDataNotFoundValue = 6005;
    public const string ReferenceDataDuplicate = "REFERENCE_DATA_DUPLICATE";
    public const int ReferenceDataDuplicateValue = 6014;

    // Organization (6006)
    public const string OrganizationMismatch = "ORGANIZATION_MISMATCH";
    public const int OrganizationMismatchValue = 6006;

    // Templates (6007)
    public const string TemplateNotFound = "TEMPLATE_NOT_FOUND";
    public const int TemplateNotFoundValue = 6007;

    // General (6008–6010)
    public const string NotFound = "NOT_FOUND";
    public const int NotFoundValue = 6008;
    public const string Conflict = "CONFLICT";
    public const int ConflictValue = 6009;
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const int ServiceUnavailableValue = 6010;

    // Retention (6013)
    public const string RetentionPeriodInvalid = "RETENTION_PERIOD_INVALID";
    public const int RetentionPeriodInvalidValue = 6013;

    // Outbox (6015)
    public const string OutboxProcessingFailed = "OUTBOX_PROCESSING_FAILED";
    public const int OutboxProcessingFailedValue = 6015;

    // Internal
    public const string InternalError = "INTERNAL_ERROR";
    public const int InternalErrorValue = 9999;
}
