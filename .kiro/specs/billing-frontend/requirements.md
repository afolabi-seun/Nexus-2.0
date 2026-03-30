# Requirements Document — Billing Frontend

## Introduction

This document defines the requirements for adding billing and subscription management pages to the existing Nexus 2.0 React frontend application. The billing frontend consumes the BillingService REST API (port 5300) to provide OrgAdmins with subscription management, plan comparison, and usage monitoring capabilities.

The billing frontend integrates into the existing feature-based module architecture under `src/frontend/src/features/billing/`, reuses shared components (DataTable, Badge, Modal, ConfirmDialog, Toast, SkeletonLoader), and follows established patterns for API clients, routing, error handling, and Zustand state management.

All requirements are derived from:
- `.kiro/specs/billing-service/requirements.md` (BillingService API endpoints and error codes)
- `.kiro/specs/billing-service/design.md` (BillingService DTOs and API specifications)
- `.kiro/specs/frontend-app/requirements.md` (Frontend architecture patterns and conventions)
- `.kiro/specs/frontend-app/design.md` (Frontend project structure and component interfaces)

## Glossary

- **Billing_Frontend**: The billing feature module within the Nexus 2.0 React frontend, located at `src/frontend/src/features/billing/`. Provides subscription management, plan comparison, and usage monitoring pages.
- **BillingService**: Backend microservice (port 5300) providing subscription management, plan tiers, feature gating, usage tracking, and payment integration via REST API.
- **BillingApi_Client**: Typed Axios instance configured with `VITE_BILLING_API_URL` base URL, using the shared `createApiClient` factory from `src/frontend/src/api/client.ts` for JWT attachment, token refresh, and error normalization.
- **BillingPage**: Primary billing management page at route `/billing`, accessible to OrgAdmin role only. Displays current subscription details, plan information, usage meters, and subscription action buttons (upgrade, downgrade, cancel).
- **PlanComparisonPage**: Plan comparison page at route `/billing/plans`, accessible to OrgAdmin role only. Displays all four plan tiers side-by-side with feature comparison, pricing, and plan selection buttons.
- **Subscription**: The organization's billing relationship with a plan tier. One subscription per organization. Has statuses: `Active`, `Trialing`, `PastDue`, `Cancelled`, `Expired`.
- **Plan**: A billing tier definition with feature limits and pricing. Four tiers: Free (tier 0), Starter (tier 1), Professional (tier 2), Enterprise (tier 3).
- **UsageMetric**: A tracked consumption metric for an organization: `active_members`, `stories_created`, or `storage_bytes`. Each metric has a `currentValue`, `limit`, and `percentUsed`.
- **Plan_Tier**: One of four levels — Free, Starter, Professional, Enterprise — each with distinct feature limits and pricing.
- **Subscription_Status**: Lifecycle state of a subscription — `Active`, `Trialing`, `PastDue`, `Cancelled`, `Expired`.
- **OrgAdmin**: Organization administrator role (permission level 100). The only role authorized to access billing pages and perform subscription actions.
- **ConfirmDialog**: Shared modal component used for destructive action confirmation (e.g., cancel subscription, downgrade plan).
- **Toast**: Shared notification component accessed via `useToast()` hook for displaying success/error messages.
- **ApiError**: Typed error class thrown by the Axios response interceptor when the backend returns an error response. Contains `errorCode`, `errorValue`, `errors`, and `correlationId`.

## Requirements

### Requirement 1: Billing API Client

**User Story:** As a developer, I want a typed API client for BillingService so that all billing HTTP communication is centralized, type-safe, and follows the same patterns as existing API clients.

#### Acceptance Criteria

1. THE Billing_Frontend SHALL create a typed Axios instance `billingApiClient` using the shared `createApiClient` factory with base URL from the `VITE_BILLING_API_URL` environment variable.
2. THE Billing_Frontend SHALL export a `billingApi` object with typed functions for all BillingService endpoints: `getPlans()`, `getCurrentSubscription()`, `createSubscription(data)`, `upgradeSubscription(data)`, `downgradeSubscription(data)`, `cancelSubscription()`, and `getUsage()`.
3. WHEN `billingApi.getPlans()` is called, THE BillingApi_Client SHALL send `GET /api/v1/plans` and return a typed `PlanResponse[]`.
4. WHEN `billingApi.getCurrentSubscription()` is called, THE BillingApi_Client SHALL send `GET /api/v1/subscriptions/current` and return a typed `SubscriptionDetailResponse`.
5. WHEN `billingApi.createSubscription(data)` is called, THE BillingApi_Client SHALL send `POST /api/v1/subscriptions` with `{ planId, paymentMethodToken }` and return a typed `SubscriptionResponse`.
6. WHEN `billingApi.upgradeSubscription(data)` is called, THE BillingApi_Client SHALL send `PATCH /api/v1/subscriptions/upgrade` with `{ newPlanId }` and return a typed `SubscriptionResponse`.
7. WHEN `billingApi.downgradeSubscription(data)` is called, THE BillingApi_Client SHALL send `PATCH /api/v1/subscriptions/downgrade` with `{ newPlanId }` and return a typed `SubscriptionResponse`.
8. WHEN `billingApi.cancelSubscription()` is called, THE BillingApi_Client SHALL send `POST /api/v1/subscriptions/cancel` and return a typed `SubscriptionResponse`.
9. WHEN `billingApi.getUsage()` is called, THE BillingApi_Client SHALL send `GET /api/v1/usage` and return a typed `UsageResponse`.
10. THE Billing_Frontend SHALL add `VITE_BILLING_API_URL` to the environment variable validation in `src/frontend/src/utils/env.ts` and expose it as `env.BILLING_API_URL`.

### Requirement 2: Billing TypeScript Types

**User Story:** As a developer, I want TypeScript types for all billing DTOs so that the billing frontend is fully type-safe and matches the BillingService backend DTOs.

#### Acceptance Criteria

1. THE Billing_Frontend SHALL define a `PlanResponse` interface with fields: `planId` (string), `planName` (string), `planCode` (string), `tierLevel` (number), `maxTeamMembers` (number), `maxDepartments` (number), `maxStoriesPerMonth` (number), `featuresJson` (string | null), `priceMonthly` (number), `priceYearly` (number).
2. THE Billing_Frontend SHALL define a `SubscriptionResponse` interface with fields: `subscriptionId` (string), `organizationId` (string), `planId` (string), `planName` (string), `planCode` (string), `status` (string), `currentPeriodStart` (string), `currentPeriodEnd` (string | null), `trialEndDate` (string | null), `cancelledAt` (string | null), `scheduledPlanId` (string | null), `scheduledPlanName` (string | null).
3. THE Billing_Frontend SHALL define a `SubscriptionDetailResponse` interface with fields: `subscription` (SubscriptionResponse), `plan` (PlanResponse), `usage` (UsageResponse).
4. THE Billing_Frontend SHALL define a `UsageResponse` interface with field: `metrics` (UsageMetric[]).
5. THE Billing_Frontend SHALL define a `UsageMetric` interface with fields: `metricName` (string), `currentValue` (number), `limit` (number), `percentUsed` (number).
6. THE Billing_Frontend SHALL define a `CreateSubscriptionRequest` interface with fields: `planId` (string), `paymentMethodToken` (string | null).
7. THE Billing_Frontend SHALL define an `UpgradeSubscriptionRequest` interface with field: `newPlanId` (string).
8. THE Billing_Frontend SHALL define a `DowngradeSubscriptionRequest` interface with field: `newPlanId` (string).
9. THE Billing_Frontend SHALL define a `PlanFeatures` interface with fields: `sprintAnalytics` (string), `customWorkflows` (boolean), `prioritySupport` (boolean), representing the parsed `FeaturesJson` structure.
10. THE Billing_Frontend SHALL define a `SubscriptionStatus` type union: `'Active' | 'Trialing' | 'PastDue' | 'Cancelled' | 'Expired'`.

### Requirement 3: Billing Routes and Navigation

**User Story:** As an OrgAdmin, I want billing pages accessible from the sidebar navigation so that I can manage my organization's subscription.

#### Acceptance Criteria

1. THE Billing_Frontend SHALL register two new routes in `src/frontend/src/router.tsx`: `/billing` (BillingPage) and `/billing/plans` (PlanComparisonPage), both wrapped in `AuthGuard` and `RoleGuard` restricted to the `OrgAdmin` role.
2. WHEN an authenticated OrgAdmin navigates to `/billing`, THE Billing_Frontend SHALL render the BillingPage component within the AppShell layout.
3. WHEN an authenticated OrgAdmin navigates to `/billing/plans`, THE Billing_Frontend SHALL render the PlanComparisonPage component within the AppShell layout.
4. WHEN a non-OrgAdmin user navigates to `/billing` or `/billing/plans`, THE Billing_Frontend SHALL redirect to `/` and display a toast "You don't have permission to access this page."
5. THE Billing_Frontend SHALL add a "Billing" navigation item to the sidebar fallback navigation array with icon `CreditCard`, path `/billing`, `sortOrder` 12, `minPermissionLevel` 100 (OrgAdmin only), and no children.
6. THE Billing_Frontend SHALL register the `CreditCard` icon from lucide-react in the Sidebar icon map so that the billing navigation item renders correctly.

### Requirement 4: Billing Page — Subscription Overview

**User Story:** As an OrgAdmin, I want to see my organization's current subscription details on the billing page so that I can understand the current plan, status, and billing period.

#### Acceptance Criteria

1. WHEN the BillingPage loads, THE Billing_Frontend SHALL call `billingApi.getCurrentSubscription()` and display the subscription details, plan information, and usage metrics.
2. WHILE the subscription data is loading, THE Billing_Frontend SHALL display a skeleton loader matching the billing page layout.
3. WHEN the subscription data loads successfully, THE Billing_Frontend SHALL display: the current plan name and plan code, the subscription status as a color-coded badge (Active=green, Trialing=blue, PastDue=orange, Cancelled=red, Expired=gray), the billing period dates (`currentPeriodStart` to `currentPeriodEnd`), and the trial end date if the subscription is in `Trialing` status.
4. WHEN the subscription has a scheduled downgrade (`scheduledPlanId` is not null), THE Billing_Frontend SHALL display a notice: "Your plan will change to {scheduledPlanName} at the end of the current billing period."
5. WHEN the subscription status is `Cancelled` and `cancelledAt` is set, THE Billing_Frontend SHALL display a notice: "Your subscription was cancelled on {cancelledAt}. Access continues until {currentPeriodEnd}."
6. IF the API call fails with `SUBSCRIPTION_NOT_FOUND` (5003), THEN THE Billing_Frontend SHALL display an empty state with message "No subscription found" and a "Choose a Plan" button that navigates to `/billing/plans`.
7. IF the API call fails with a network or unexpected error, THEN THE Billing_Frontend SHALL display an error state with a "Retry" button that re-fetches the subscription data.

### Requirement 5: Billing Page — Usage Dashboard

**User Story:** As an OrgAdmin, I want to see visual usage meters on the billing page so that I can monitor my organization's consumption against plan limits.

#### Acceptance Criteria

1. WHEN the subscription data loads successfully, THE Billing_Frontend SHALL display a usage dashboard section showing usage meters for each metric returned by the API: `active_members`, `stories_created`, and `storage_bytes`.
2. THE Billing_Frontend SHALL render each usage metric as a labeled progress bar showing: the metric display name (e.g., "Active Members", "Stories Created", "Storage"), the current value and limit (e.g., "12 / 25"), the percentage used as a visual progress bar fill, and a percentage label (e.g., "48%").
3. WHEN a usage metric's `limit` is 0 (unlimited), THE Billing_Frontend SHALL display "Unlimited" instead of a numeric limit and render the progress bar as empty with a label of "Unlimited".
4. WHEN a usage metric's `percentUsed` exceeds 80, THE Billing_Frontend SHALL render the progress bar in a warning color (amber/yellow).
5. WHEN a usage metric's `percentUsed` exceeds 95, THE Billing_Frontend SHALL render the progress bar in a danger color (red).
6. WHEN a usage metric's `percentUsed` is 80 or below, THE Billing_Frontend SHALL render the progress bar in the default color (primary/blue).
7. THE Billing_Frontend SHALL display the `storage_bytes` metric with human-readable formatting (e.g., "1.2 GB / 5 GB") by converting bytes to the appropriate unit (KB, MB, GB).

### Requirement 6: Billing Page — Subscription Actions

**User Story:** As an OrgAdmin, I want to upgrade, downgrade, or cancel my subscription from the billing page so that I can manage my organization's plan.

#### Acceptance Criteria

1. WHEN the subscription is `Active` or `Trialing`, THE Billing_Frontend SHALL display an "Upgrade Plan" button and a "Change Plan" link that both navigate to `/billing/plans`.
2. WHEN the subscription is `Active` or `Trialing` and the current plan is not the Free plan, THE Billing_Frontend SHALL display a "Cancel Subscription" button.
3. WHEN the user clicks "Cancel Subscription", THE Billing_Frontend SHALL open a ConfirmDialog with title "Cancel Subscription", message "Are you sure you want to cancel? Your access will continue until the end of the current billing period.", and a destructive "Cancel Subscription" confirm button.
4. WHEN the user confirms cancellation, THE Billing_Frontend SHALL call `billingApi.cancelSubscription()`, display a success toast "Subscription cancelled", and refresh the subscription data.
5. IF cancellation fails with `SUBSCRIPTION_ALREADY_CANCELLED` (5008), THEN THE Billing_Frontend SHALL display an error toast "Subscription is already cancelled."
6. IF cancellation fails with `NO_ACTIVE_SUBSCRIPTION` (5005), THEN THE Billing_Frontend SHALL display an error toast "No active subscription found."
7. WHEN the subscription status is `Cancelled`, `Expired`, or `PastDue`, THE Billing_Frontend SHALL hide the "Cancel Subscription" button.
8. WHEN the subscription status is `Cancelled` or `Expired`, THE Billing_Frontend SHALL display a "Resubscribe" button that navigates to `/billing/plans`.
9. WHILE a subscription action (cancel) is in progress, THE Billing_Frontend SHALL disable the action button and show a loading indicator to prevent duplicate submissions.

### Requirement 7: Plan Comparison Page

**User Story:** As an OrgAdmin, I want to compare all available plans side-by-side so that I can make an informed decision about which plan to choose.

#### Acceptance Criteria

1. WHEN the PlanComparisonPage loads, THE Billing_Frontend SHALL call `billingApi.getPlans()` and `billingApi.getCurrentSubscription()` in parallel to fetch all active plans and the current subscription.
2. WHILE the plan data is loading, THE Billing_Frontend SHALL display a skeleton loader matching the plan comparison layout.
3. WHEN the plan data loads successfully, THE Billing_Frontend SHALL display all plans in a side-by-side grid (4 columns on desktop, stacked on mobile) ordered by `tierLevel` ascending (Free, Starter, Professional, Enterprise).
4. THE Billing_Frontend SHALL display for each plan: the plan name, monthly price (formatted as "$X.XX/mo" or "Free"), yearly price (formatted as "$X.XX/yr"), and the following feature rows: Max Team Members, Max Departments, Max Stories/Month, Sprint Analytics, Custom Workflows, and Priority Support.
5. WHEN a plan feature limit is 0 (unlimited), THE Billing_Frontend SHALL display "Unlimited" in the feature row.
6. WHEN a plan feature is a boolean (`customWorkflows`, `prioritySupport`), THE Billing_Frontend SHALL display a check icon for `true` and a dash or X icon for `false`.
7. WHEN a plan feature is a string (`sprintAnalytics`), THE Billing_Frontend SHALL display the capitalized value (e.g., "None", "Basic", "Full").
8. THE Billing_Frontend SHALL visually highlight the user's current plan with a "Current Plan" badge and a distinct border or background color.
9. WHEN the user's current subscription exists and is `Active` or `Trialing`, THE Billing_Frontend SHALL display an "Upgrade" button on plans with a higher `tierLevel` than the current plan, and a "Downgrade" button on plans with a lower `tierLevel`.
10. WHEN the user has no subscription or the subscription is `Cancelled`/`Expired`, THE Billing_Frontend SHALL display a "Select Plan" button on all plans.
11. THE Billing_Frontend SHALL parse each plan's `featuresJson` field into a `PlanFeatures` object for rendering the Sprint Analytics, Custom Workflows, and Priority Support rows.

### Requirement 8: Plan Selection — Upgrade

**User Story:** As an OrgAdmin, I want to upgrade my plan from the plan comparison page so that my organization can access more features.

#### Acceptance Criteria

1. WHEN the user clicks "Upgrade" on a plan, THE Billing_Frontend SHALL open a ConfirmDialog with title "Upgrade to {planName}", message "You will be upgraded immediately. Prorated charges will apply for the remainder of the current billing period.", and a non-destructive "Confirm Upgrade" button.
2. WHEN the user confirms the upgrade, THE Billing_Frontend SHALL call `billingApi.upgradeSubscription({ newPlanId })`, display a success toast "Plan upgraded to {planName}", and navigate to `/billing`.
3. IF the upgrade fails with `INVALID_UPGRADE_PATH` (5004), THEN THE Billing_Frontend SHALL display an error toast "Cannot upgrade to this plan. It must be a higher tier than your current plan."
4. IF the upgrade fails with `NO_ACTIVE_SUBSCRIPTION` (5005), THEN THE Billing_Frontend SHALL display an error toast "No active subscription found."
5. IF the upgrade fails with `PAYMENT_PROVIDER_ERROR` (5010), THEN THE Billing_Frontend SHALL display an error toast "Payment processing failed. Please try again or contact support."
6. WHILE the upgrade request is in progress, THE Billing_Frontend SHALL disable the confirm button and show a loading indicator.

### Requirement 9: Plan Selection — Downgrade

**User Story:** As an OrgAdmin, I want to downgrade my plan from the plan comparison page so that I can reduce costs when fewer features are needed.

#### Acceptance Criteria

1. WHEN the user clicks "Downgrade" on a plan, THE Billing_Frontend SHALL open a ConfirmDialog with title "Downgrade to {planName}", message "The downgrade will take effect at the end of your current billing period. Your current plan features will remain available until then.", and a destructive "Confirm Downgrade" button.
2. WHEN the user confirms the downgrade, THE Billing_Frontend SHALL call `billingApi.downgradeSubscription({ newPlanId })`, display a success toast "Downgrade to {planName} scheduled", and navigate to `/billing`.
3. IF the downgrade fails with `INVALID_DOWNGRADE_PATH` (5006), THEN THE Billing_Frontend SHALL display an error toast "Cannot downgrade to this plan. It must be a lower tier than your current plan."
4. IF the downgrade fails with `USAGE_EXCEEDS_PLAN_LIMITS` (5007), THEN THE Billing_Frontend SHALL display an error toast "Your current usage exceeds the limits of the selected plan. Reduce usage before downgrading."
5. IF the downgrade fails with `NO_ACTIVE_SUBSCRIPTION` (5005), THEN THE Billing_Frontend SHALL display an error toast "No active subscription found."
6. WHILE the downgrade request is in progress, THE Billing_Frontend SHALL disable the confirm button and show a loading indicator.

### Requirement 10: Plan Selection — New Subscription

**User Story:** As an OrgAdmin without a subscription, I want to select a plan so that my organization can start using paid features.

#### Acceptance Criteria

1. WHEN the user clicks "Select Plan" on a plan (when no active subscription exists), THE Billing_Frontend SHALL call `billingApi.createSubscription({ planId, paymentMethodToken: null })`, display a success toast "Subscribed to {planName}", and navigate to `/billing`.
2. IF subscription creation fails with `SUBSCRIPTION_ALREADY_EXISTS` (5001), THEN THE Billing_Frontend SHALL display an error toast "Your organization already has an active subscription."
3. IF subscription creation fails with `PLAN_NOT_FOUND` (5002), THEN THE Billing_Frontend SHALL display an error toast "The selected plan is no longer available."
4. IF subscription creation fails with `PAYMENT_PROVIDER_ERROR` (5010), THEN THE Billing_Frontend SHALL display an error toast "Payment processing failed. Please try again or contact support."
5. WHILE the subscription creation request is in progress, THE Billing_Frontend SHALL disable the "Select Plan" button and show a loading indicator.

### Requirement 11: Billing Error Code Mapping

**User Story:** As a developer, I want all billing error codes mapped to user-friendly messages so that billing errors are displayed consistently with the rest of the application.

#### Acceptance Criteria

1. THE Billing_Frontend SHALL add the following error code mappings to the existing `errorCodeMap` in `src/frontend/src/utils/errorMapping.ts`:

| Error Code | User Message |
|---|---|
| `SUBSCRIPTION_ALREADY_EXISTS` | "Your organization already has an active subscription." |
| `PLAN_NOT_FOUND` | "The selected plan is no longer available." |
| `SUBSCRIPTION_NOT_FOUND` | "No subscription found for your organization." |
| `INVALID_UPGRADE_PATH` | "Cannot upgrade to this plan. It must be a higher tier than your current plan." |
| `NO_ACTIVE_SUBSCRIPTION` | "No active subscription found." |
| `INVALID_DOWNGRADE_PATH` | "Cannot downgrade to this plan. It must be a lower tier than your current plan." |
| `USAGE_EXCEEDS_PLAN_LIMITS` | "Your current usage exceeds the limits of the selected plan. Reduce usage before downgrading." |
| `SUBSCRIPTION_ALREADY_CANCELLED` | "Subscription is already cancelled." |
| `TRIAL_EXPIRED` | "Your trial period has ended." |
| `PAYMENT_PROVIDER_ERROR` | "Payment processing failed. Please try again or contact support." |
| `FEATURE_NOT_AVAILABLE` | "This feature is not included in your current plan." |
| `USAGE_LIMIT_REACHED` | "Your organization has reached the usage limit for this feature." |

2. WHEN a billing API call fails with an `ApiError`, THE Billing_Frontend SHALL use the existing `mapErrorCode()` function to resolve the user-friendly message and display it via the Toast component.

### Requirement 12: Billing Page — Current Plan Details Card

**User Story:** As an OrgAdmin, I want to see my current plan's feature limits on the billing page so that I understand what is included in my subscription.

#### Acceptance Criteria

1. WHEN the subscription data loads successfully, THE Billing_Frontend SHALL display a plan details card showing: the plan name, the monthly and yearly pricing, and the feature limits (Max Team Members, Max Departments, Max Stories/Month, Sprint Analytics, Custom Workflows, Priority Support).
2. WHEN a feature limit is 0 (unlimited), THE Billing_Frontend SHALL display "Unlimited" for that feature.
3. WHEN a feature is a boolean, THE Billing_Frontend SHALL display "Included" for `true` and "Not included" for `false`.
4. THE Billing_Frontend SHALL parse the plan's `featuresJson` field to display Sprint Analytics level, Custom Workflows availability, and Priority Support availability.
5. THE Billing_Frontend SHALL display a "Compare Plans" link that navigates to `/billing/plans`.

### Requirement 13: Responsive Layout

**User Story:** As an OrgAdmin, I want the billing pages to be responsive so that I can manage billing on different screen sizes.

#### Acceptance Criteria

1. WHEN the viewport width is 1024px or wider, THE PlanComparisonPage SHALL display plans in a 4-column grid layout.
2. WHEN the viewport width is between 768px and 1023px, THE PlanComparisonPage SHALL display plans in a 2-column grid layout.
3. WHEN the viewport width is below 768px, THE PlanComparisonPage SHALL display plans in a single-column stacked layout.
4. THE BillingPage SHALL use a responsive layout with the subscription overview, plan details, and usage dashboard sections stacking vertically on narrow viewports and arranging in a grid on wider viewports.
