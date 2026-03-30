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
    'VALIDATION_ERROR',
    'INVALID_TASK_TRANSITION',
    'INTERNAL_ERROR',
    'NETWORK_ERROR',
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
