namespace BillingService.Domain.Exceptions;

public static class ErrorCodes
{
    // Shared
    public const string ValidationError = "VALIDATION_ERROR";
    public const int ValidationErrorValue = 1000;

    // Billing (5001–5014)
    public const string SubscriptionAlreadyExists = "SUBSCRIPTION_ALREADY_EXISTS";
    public const int SubscriptionAlreadyExistsValue = 5001;

    public const string PlanNotFound = "PLAN_NOT_FOUND";
    public const int PlanNotFoundValue = 5002;

    public const string SubscriptionNotFound = "SUBSCRIPTION_NOT_FOUND";
    public const int SubscriptionNotFoundValue = 5003;

    public const string InvalidUpgradePath = "INVALID_UPGRADE_PATH";
    public const int InvalidUpgradePathValue = 5004;

    public const string NoActiveSubscription = "NO_ACTIVE_SUBSCRIPTION";
    public const int NoActiveSubscriptionValue = 5005;

    public const string InvalidDowngradePath = "INVALID_DOWNGRADE_PATH";
    public const int InvalidDowngradePathValue = 5006;

    public const string UsageExceedsPlanLimits = "USAGE_EXCEEDS_PLAN_LIMITS";
    public const int UsageExceedsPlanLimitsValue = 5007;

    public const string SubscriptionAlreadyCancelled = "SUBSCRIPTION_ALREADY_CANCELLED";
    public const int SubscriptionAlreadyCancelledValue = 5008;

    public const string TrialExpired = "TRIAL_EXPIRED";
    public const int TrialExpiredValue = 5009;

    public const string PaymentProviderError = "PAYMENT_PROVIDER_ERROR";
    public const int PaymentProviderErrorValue = 5010;

    public const string InvalidWebhookSignature = "INVALID_WEBHOOK_SIGNATURE";
    public const int InvalidWebhookSignatureValue = 5011;

    public const string InvalidWebhookPayload = "INVALID_WEBHOOK_PAYLOAD";
    public const int InvalidWebhookPayloadValue = 5012;

    public const string FeatureNotAvailable = "FEATURE_NOT_AVAILABLE";
    public const int FeatureNotAvailableValue = 5013;

    public const string UsageLimitReached = "USAGE_LIMIT_REACHED";
    public const int UsageLimitReachedValue = 5014;

    public const string PlanAlreadyExists = "PLAN_ALREADY_EXISTS";
    public const int PlanAlreadyExistsValue = 5015;

    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
    public const int InsufficientPermissionsValue = 5016;

    public const string PlanCodeImmutable = "PLAN_CODE_IMMUTABLE";
    public const int PlanCodeImmutableValue = 5017;

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
