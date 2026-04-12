import { ApiError } from '@/types/api';

const errorCodeMap: Record<string, string> = {
    INVALID_CREDENTIALS: 'Invalid email or password',
    ACCOUNT_LOCKED: 'Account locked. Try again later.',
    ACCOUNT_INACTIVE: 'Account is inactive. Contact your administrator.',
    SUSPICIOUS_LOGIN:
        'Login blocked due to suspicious activity. Check your email for verification.',
    PASSWORD_COMPLEXITY_FAILED:
        'Password must be at least 8 characters with 1 uppercase, 1 lowercase, 1 digit, and 1 special character (!@#$%^&*).',
    PASSWORD_REUSE_NOT_ALLOWED:
        'New password cannot be the same as the temporary password.',
    PASSWORD_RECENTLY_USED:
        'This password was recently used. Choose a different one.',
    OTP_EXPIRED: 'Code expired. Request a new one.',
    OTP_MAX_ATTEMPTS: 'Too many attempts. Request a new code.',
    REFRESH_TOKEN_REUSE: 'Session expired. Please log in again.',
    PROJECT_KEY_DUPLICATE: 'This project key is already in use.',
    PROJECT_NAME_DUPLICATE: 'A project with this name already exists.',
    INVALID_STORY_TRANSITION: 'Invalid status transition.',
    STORY_REQUIRES_ASSIGNEE:
        'Story must have an assignee before this transition.',
    STORY_REQUIRES_TASKS: 'Story must have tasks before this transition.',
    STORY_REQUIRES_POINTS:
        'Story must have story points before this transition.',
    INVALID_STORY_POINTS:
        'Story points must be a Fibonacci number (1, 2, 3, 5, 8, 13, 21).',
    INVALID_PRIORITY: 'Invalid priority value.',
    ASSIGNEE_NOT_IN_DEPARTMENT:
        'Selected member is not in the required department.',
    ASSIGNEE_AT_CAPACITY:
        'Selected member has reached their maximum concurrent tasks.',
    LABEL_NAME_DUPLICATE: 'A label with this name already exists.',
    COMMENT_NOT_AUTHOR:
        'Only the comment author can edit or delete this comment.',
    MENTION_USER_NOT_FOUND:
        'One or more mentioned users were not found.',
    SPRINT_END_BEFORE_START: 'End date must be after start date.',
    ONLY_ONE_ACTIVE_SPRINT:
        'Another sprint is already active for this project.',
    STORY_ALREADY_IN_SPRINT: 'This story is already in the sprint.',
    SPRINT_NOT_IN_PLANNING:
        'Stories can only be added during sprint planning.',
    STORY_PROJECT_MISMATCH: 'This story belongs to a different project.',
    MEMBER_ALREADY_IN_DEPARTMENT: 'Member is already in this department.',
    LAST_ORGADMIN_CANNOT_DEACTIVATE:
        'Cannot deactivate the last OrgAdmin.',
    DEPARTMENT_NAME_DUPLICATE:
        'A department with this name already exists.',
    DEPARTMENT_CODE_DUPLICATE:
        'A department with this code already exists.',
    STORY_PREFIX_INVALID_FORMAT:
        'Story ID prefix must be 2–10 uppercase alphanumeric characters.',
    STORY_PREFIX_DUPLICATE:
        'This story ID prefix is already in use by another organization.',
    STORY_PREFIX_IMMUTABLE:
        'Story ID prefix cannot be changed after stories have been created.',
    INVALID_PREFERENCE_VALUE: 'Invalid preference value.',
    INVITE_EMAIL_ALREADY_MEMBER:
        'This email is already registered as a member.',
    INVITE_EXPIRED_OR_INVALID:
        'This invitation has expired or is no longer valid.',
    MAX_DEVICES_REACHED:
        'Maximum of 5 devices reached. Remove a device first.',
    ORGANIZATION_NAME_DUPLICATE:
        'An organization with this name already exists.',
    SUBSCRIPTION_ALREADY_EXISTS:
        'Your organization already has an active subscription.',
    PLAN_NOT_FOUND:
        'The selected plan is no longer available.',
    SUBSCRIPTION_NOT_FOUND:
        'No subscription found for your organization.',
    INVALID_UPGRADE_PATH:
        'Cannot upgrade to this plan. It must be a higher tier than your current plan.',
    NO_ACTIVE_SUBSCRIPTION:
        'No active subscription found.',
    INVALID_DOWNGRADE_PATH:
        'Cannot downgrade to this plan. It must be a lower tier than your current plan.',
    USAGE_EXCEEDS_PLAN_LIMITS:
        'Your current usage exceeds the limits of the selected plan. Reduce usage before downgrading.',
    SUBSCRIPTION_ALREADY_CANCELLED:
        'Subscription is already cancelled.',
    TRIAL_EXPIRED:
        'Your trial period has ended.',
    PAYMENT_PROVIDER_ERROR:
        'Payment processing failed. Please try again or contact support.',
    FEATURE_NOT_AVAILABLE:
        'This feature is not included in your current plan.',
    USAGE_LIMIT_REACHED:
        'Your organization has reached the usage limit for this feature.',
    VALIDATION_ERROR: 'Please fix the highlighted fields.',
    INVALID_TASK_TRANSITION: 'Invalid task status transition.',
    INSUFFICIENT_PERMISSIONS: 'You don\'t have permission to perform this action.',
    ORGADMIN_REQUIRED: 'This action requires OrgAdmin access.',
    DEPTLEAD_REQUIRED: 'This action requires DeptLead or higher access.',
    PLATFORM_ADMIN_REQUIRED: 'This action requires PlatformAdmin access.',
    INTERNAL_ERROR: 'Something went wrong. Please try again.',
    NETWORK_ERROR:
        'Unable to connect to the server. Please check your connection.',
};

const FALLBACK_MESSAGE = 'Something went wrong. Please try again.';

export function mapErrorCode(errorCode: string): string {
    return errorCodeMap[errorCode] ?? FALLBACK_MESSAGE;
}

export function mapApiError(error: ApiError): {
    message: string;
    fieldErrors?: Record<string, string>;
} {
    const message = mapErrorCode(error.errorCode);

    if (error.errors && error.errors.length > 0) {
        const fieldErrors: Record<string, string> = {};
        for (const e of error.errors) {
            fieldErrors[e.field] = e.message;
        }
        return { message, fieldErrors };
    }

    return { message };
}
