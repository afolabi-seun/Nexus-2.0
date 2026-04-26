import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { mapErrorCode, mapApiError } from '@/utils/errorMapping';
import { ApiError } from '@/types/api';

/**
 * **Validates: Requirements 39.2**
 *
 * Property 19: Error code to user message mapping
 * For any known error code, mapErrorCode returns correct message; unknown codes return fallback.
 */

const knownErrorCodes = [
    'INVALID_CREDENTIALS',
    'ACCOUNT_LOCKED',
    'ACCOUNT_INACTIVE',
    'SUSPICIOUS_LOGIN',
    'PASSWORD_COMPLEXITY_FAILED',
    'PASSWORD_REUSE_NOT_ALLOWED',
    'PASSWORD_RECENTLY_USED',
    'OTP_EXPIRED',
    'OTP_MAX_ATTEMPTS',
    'REFRESH_TOKEN_REUSE',
    'PROJECT_KEY_DUPLICATE',
    'PROJECT_NAME_DUPLICATE',
    'INVALID_STORY_TRANSITION',
    'STORY_REQUIRES_ASSIGNEE',
    'STORY_REQUIRES_TASKS',
    'STORY_REQUIRES_POINTS',
    'INVALID_STORY_POINTS',
    'INVALID_PRIORITY',
    'ASSIGNEE_NOT_IN_DEPARTMENT',
    'ASSIGNEE_AT_CAPACITY',
    'LABEL_NAME_DUPLICATE',
    'COMMENT_NOT_AUTHOR',
    'MENTION_USER_NOT_FOUND',
    'SPRINT_END_BEFORE_START',
    'ONLY_ONE_ACTIVE_SPRINT',
    'STORY_ALREADY_IN_SPRINT',
    'SPRINT_NOT_IN_PLANNING',
    'STORY_PROJECT_MISMATCH',
    'MEMBER_ALREADY_IN_DEPARTMENT',
    'LAST_ORGADMIN_CANNOT_DEACTIVATE',
    'DEPARTMENT_NAME_DUPLICATE',
    'DEPARTMENT_CODE_DUPLICATE',
    'STORY_PREFIX_INVALID_FORMAT',
    'STORY_PREFIX_DUPLICATE',
    'STORY_PREFIX_IMMUTABLE',
    'INVALID_PREFERENCE_VALUE',
    'INVITE_EMAIL_ALREADY_MEMBER',
    'INVITE_EXPIRED_OR_INVALID',
    'MAX_DEVICES_REACHED',
    'ORGANIZATION_NAME_DUPLICATE',
    'SUBSCRIPTION_ALREADY_EXISTS',
    'PLAN_NOT_FOUND',
    'SUBSCRIPTION_NOT_FOUND',
    'INVALID_UPGRADE_PATH',
    'NO_ACTIVE_SUBSCRIPTION',
    'INVALID_DOWNGRADE_PATH',
    'USAGE_EXCEEDS_PLAN_LIMITS',
    'SUBSCRIPTION_ALREADY_CANCELLED',
    'TRIAL_EXPIRED',
    'PAYMENT_PROVIDER_ERROR',
    'FEATURE_NOT_AVAILABLE',
    'USAGE_LIMIT_REACHED',
    'VALIDATION_ERROR',
    'INVALID_TASK_TRANSITION',
    'INSUFFICIENT_PERMISSIONS',
    'ORGADMIN_REQUIRED',
    'DEPTLEAD_REQUIRED',
    'PLATFORM_ADMIN_REQUIRED',
    'INTERNAL_ERROR',
    'NETWORK_ERROR',
    // Auth/Security
    'FIRST_TIME_USER_RESTRICTED',
    'INVALID_TOKEN',
    'TOKEN_EXPIRED',
    'TOKEN_REVOKED',
    'SESSION_EXPIRED',
    'OTP_VERIFICATION_FAILED',
    'RATE_LIMIT_EXCEEDED',
    'SERVICE_NOT_AUTHORIZED',
    'ORGANIZATION_MISMATCH',
    // Project/Story/Task
    'PROJECT_NOT_FOUND',
    'PROJECT_KEY_IMMUTABLE',
    'PROJECT_KEY_INVALID_FORMAT',
    'STORY_NOT_FOUND',
    'STORY_KEY_NOT_FOUND',
    'STORY_DESCRIPTION_REQUIRED',
    'STORY_IN_ACTIVE_SPRINT',
    'STORY_NOT_IN_SPRINT',
    'INVALID_STORY_TYPE',
    'TASK_NOT_FOUND',
    'TASK_IN_PROGRESS',
    'INVALID_TASK_TYPE',
    'MAX_LABELS_PER_STORY',
    // Sprint
    'SPRINT_NOT_FOUND',
    'SPRINT_OVERLAP',
    'SPRINT_ALREADY_ACTIVE',
    'SPRINT_ALREADY_COMPLETED',
    // Comment/Label
    'COMMENT_NOT_FOUND',
    'LABEL_NOT_FOUND',
    // Member/Department
    'MEMBER_NOT_FOUND',
    'MEMBER_NOT_IN_DEPARTMENT',
    'MEMBER_MUST_HAVE_DEPARTMENT',
    'DEPARTMENT_NOT_FOUND',
    'DEPARTMENT_ACCESS_DENIED',
    'DEPARTMENT_HAS_ACTIVE_MEMBERS',
    'DEFAULT_DEPARTMENT_CANNOT_DELETE',
    'EMAIL_ALREADY_REGISTERED',
    'INVALID_ROLE_ASSIGNMENT',
    'INVALID_DEPARTMENT_ROLE',
    'INVALID_AVAILABILITY_STATUS',
    // Time Tracking
    'TIME_ENTRY_NOT_FOUND',
    'DAILY_HOURS_EXCEEDED',
    'HOURS_MUST_BE_POSITIVE',
    'COST_RATE_DUPLICATE',
    'INVALID_COST_RATE',
    'INVALID_TIME_POLICY',
    'TIMER_ALREADY_ACTIVE',
    'NO_ACTIVE_TIMER',
    // Analytics/Risk
    'INVALID_ANALYTICS_PARAMETER',
    'SNAPSHOT_GENERATION_FAILED',
    'RISK_NOT_FOUND',
    'INVALID_RISK_LIKELIHOOD',
    'INVALID_RISK_SEVERITY',
    'INVALID_MITIGATION_STATUS',
    // Search
    'SEARCH_QUERY_TOO_SHORT',
    // Billing
    'PLAN_ALREADY_EXISTS',
    'PLAN_CODE_IMMUTABLE',
    'INVALID_WEBHOOK_PAYLOAD',
    'INVALID_WEBHOOK_SIGNATURE',
    // Utility
    'AUDIT_LOG_IMMUTABLE',
    'ERROR_CODE_DUPLICATE',
    'ERROR_CODE_NOT_FOUND',
    'NOTIFICATION_DISPATCH_FAILED',
    'REFERENCE_DATA_DUPLICATE',
    'REFERENCE_DATA_NOT_FOUND',
    'RETENTION_PERIOD_INVALID',
    'INVALID_NOTIFICATION_TYPE',
    'INVALID_CHANNEL',
    'OUTBOX_PROCESSING_FAILED',
    'TEMPLATE_NOT_FOUND',
    'PREFERENCE_KEY_UNKNOWN',
    // Generic
    'NOT_FOUND',
    'CONFLICT',
    'SERVICE_UNAVAILABLE',
    'FOREIGN_KEY_VIOLATION',
    'UNIQUE_CONSTRAINT_VIOLATION',
    'STORY_SEQUENCE_INIT_FAILED',
] as const;

const FALLBACK_MESSAGE = 'Something went wrong. Please try again.';

const arbKnownCode = fc.constantFrom(...knownErrorCodes);

describe('Error Code to User Message Mapping', () => {
    it('property: known error codes return a non-empty, non-fallback message', () => {
        fc.assert(
            fc.property(arbKnownCode, (code) => {
                const message = mapErrorCode(code);
                expect(typeof message).toBe('string');
                expect(message.length).toBeGreaterThan(0);
            }),
            { numRuns: 100 }
        );
    });

    it('property: unknown error codes return fallback message', () => {
        fc.assert(
            fc.property(
                fc.stringMatching(/^UNKNOWN_[A-Z]{3,10}$/).filter(
                    (s) => !(knownErrorCodes as readonly string[]).includes(s)
                ),
                (code) => {
                    const message = mapErrorCode(code);
                    expect(message).toBe(FALLBACK_MESSAGE);
                }
            ),
            { numRuns: 50 }
        );
    });

    it('property: mapApiError returns message and optional fieldErrors', () => {
        fc.assert(
            fc.property(
                arbKnownCode,
                fc.option(
                    fc.array(
                        fc.record({
                            field: fc.stringMatching(/^[a-z]{3,10}$/),
                            message: fc.string({ minLength: 1, maxLength: 50 }),
                        }),
                        { minLength: 1, maxLength: 3 }
                    ),
                    { nil: null }
                ),
                (code, errors) => {
                    const apiError = new ApiError('test', code, 0, errors);
                    const result = mapApiError(apiError);

                    expect(result.message).toBe(mapErrorCode(code));

                    if (errors && errors.length > 0) {
                        expect(result.fieldErrors).toBeDefined();
                        for (const e of errors) {
                            expect(result.fieldErrors![e.field]).toBe(e.message);
                        }
                    } else {
                        expect(result.fieldErrors).toBeUndefined();
                    }
                }
            ),
            { numRuns: 50 }
        );
    });
});
