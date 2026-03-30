# Implementation Plan: Billing Frontend

## Overview

Add billing and subscription management pages to the existing Nexus 2.0 React frontend. The implementation proceeds incrementally: foundation types and API client first, then shared component updates (sidebar, router, Badge, error mappings), then billing-specific components, then pages that wire everything together, and finally tests and build verification.

## Tasks

- [x] 1. Foundation — Types, API client, environment config, and error mappings
  - [x] 1.1 Create billing TypeScript types at `src/frontend/src/types/billing.ts`
    - Define all interfaces: `PlanResponse`, `PlanFeatures`, `SubscriptionResponse`, `SubscriptionDetailResponse`, `UsageResponse`, `UsageMetric`, `CreateSubscriptionRequest`, `UpgradeSubscriptionRequest`, `DowngradeSubscriptionRequest`, and `SubscriptionStatus` type union
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10_

  - [x] 1.2 Add `VITE_BILLING_API_URL` to environment config
    - Add `'VITE_BILLING_API_URL'` to the `REQUIRED_VARS` array in `src/frontend/src/utils/env.ts`
    - Add `BILLING_API_URL: import.meta.env.VITE_BILLING_API_URL ?? ''` to the `env` object
    - Add `VITE_BILLING_API_URL=http://localhost:5300` to `src/frontend/.env.example`
    - _Requirements: 1.10_

  - [x] 1.3 Create billing API client at `src/frontend/src/api/billingApi.ts`
    - Use `createApiClient` factory with `env.BILLING_API_URL` base URL
    - Export `billingApi` object with typed functions: `getPlans`, `getCurrentSubscription`, `createSubscription`, `upgradeSubscription`, `downgradeSubscription`, `cancelSubscription`, `getUsage`
    - Each function calls the correct HTTP method and path, returns the typed response
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9_

  - [x] 1.4 Add billing error codes to `src/frontend/src/utils/errorMapping.ts`
    - Add all 12 billing error code entries to `errorCodeMap`: SUBSCRIPTION_ALREADY_EXISTS, PLAN_NOT_FOUND, SUBSCRIPTION_NOT_FOUND, INVALID_UPGRADE_PATH, NO_ACTIVE_SUBSCRIPTION, INVALID_DOWNGRADE_PATH, USAGE_EXCEEDS_PLAN_LIMITS, SUBSCRIPTION_ALREADY_CANCELLED, TRIAL_EXPIRED, PAYMENT_PROVIDER_ERROR, FEATURE_NOT_AVAILABLE, USAGE_LIMIT_REACHED
    - _Requirements: 11.1_

  - [ ]* 1.5 Write property test for billing error code mapping completeness
    - **Property 7: Billing error code mapping completeness**
    - **Validates: Requirements 11.1**

- [x] 2. Shared component updates — Sidebar, Router, Badge
  - [x] 2.1 Add billing subscription status colors to Badge component
    - Add `Trialing` (blue), `PastDue` (orange), and `Expired` (gray) entries to `statusColors` in `src/frontend/src/components/common/Badge.tsx`
    - _Requirements: 4.3_

  - [x] 2.2 Add billing navigation item to Sidebar
    - Import `CreditCard` from lucide-react and add it to the `iconMap` in `src/frontend/src/components/layout/Sidebar.tsx`
    - Add a `Billing` entry to `fallbackNavigation` with `sortOrder: 12`, `minPermissionLevel: 100`, `icon: 'CreditCard'`, `path: '/billing'`
    - _Requirements: 3.5, 3.6_

  - [x] 2.3 Register billing routes in router
    - Import `BillingPage` and `PlanComparisonPage` (lazy or direct)
    - Add `/billing` and `/billing/plans` routes inside the existing `AuthGuard > AppShell > RoleGuard(OrgAdmin)` children block
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 3. Checkpoint — Verify foundation compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Billing utility and presentational components
  - [x] 4.1 Create `formatBytes` utility at `src/frontend/src/features/billing/utils/formatBytes.ts`
    - Convert bytes to human-readable string (B, KB, MB, GB, TB) using 1024-based units
    - _Requirements: 5.7_

  - [ ]* 4.2 Write property test for `formatBytes`
    - **Property 2: formatBytes produces correct human-readable output**
    - **Validates: Requirements 5.7**

  - [x] 4.3 Create `UsageMeter` component at `src/frontend/src/features/billing/components/UsageMeter.tsx`
    - Render metric display name, current/limit values ("X / Y" or "X / Unlimited"), progress bar with color coding (blue ≤80%, amber >80%, red >95%), percentage label
    - Use `formatBytes` for `storage_bytes` metric
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

  - [ ]* 4.4 Write property test for UsageMeter color coding
    - **Property 1: Usage meter color coding by threshold**
    - **Validates: Requirements 5.4, 5.5, 5.6**

  - [ ]* 4.5 Write property test for UsageMeter rendering completeness
    - **Property 8: Usage meter renders all required information**
    - **Validates: Requirements 5.1, 5.2, 5.3**

  - [x] 4.6 Create `PlanDetailsCard` component at `src/frontend/src/features/billing/components/PlanDetailsCard.tsx`
    - Display plan name, monthly/yearly pricing, feature limits table (max members, depts, stories/month, sprint analytics, custom workflows, priority support)
    - Parse `featuresJson`, display "Unlimited" for 0 limits, "Included"/"Not included" for booleans
    - Include "Compare Plans" link to `/billing/plans`
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_

  - [ ]* 4.7 Write property test for featuresJson round-trip parsing
    - **Property 6: featuresJson round-trip parsing**
    - **Validates: Requirements 7.11, 12.4**

  - [x] 4.8 Create `SubscriptionOverview` component at `src/frontend/src/features/billing/components/SubscriptionOverview.tsx`
    - Display plan name, status Badge, billing period dates, trial end date (if Trialing), scheduled downgrade notice, cancellation notice
    - _Requirements: 4.3, 4.4, 4.5_

  - [x] 4.9 Create `SubscriptionActions` component at `src/frontend/src/features/billing/components/SubscriptionActions.tsx`
    - Render action buttons based on subscription status and plan: "Upgrade Plan" / "Change Plan" links, "Cancel Subscription" button with ConfirmDialog, "Resubscribe" link
    - Handle cancel flow: ConfirmDialog → `billingApi.cancelSubscription()` → success toast → `onCancelSuccess` callback
    - Map cancel errors (SUBSCRIPTION_ALREADY_CANCELLED, NO_ACTIVE_SUBSCRIPTION) to error toasts
    - Disable button + show loading indicator while action is in progress
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9_

  - [ ]* 4.10 Write property test for subscription action button visibility
    - **Property 3: Subscription action button visibility by status and plan**
    - **Validates: Requirements 6.1, 6.2, 6.7, 6.8**

  - [x] 4.11 Create `PlanCard` component at `src/frontend/src/features/billing/components/PlanCard.tsx`
    - Display plan name, pricing, feature rows (limits, booleans as check/X icons, string values capitalized)
    - Show "Current Plan" badge + distinct border for current plan
    - Render correct action button: "Upgrade", "Downgrade", "Select Plan", or "Current Plan" (disabled)
    - Parse `featuresJson` for Sprint Analytics, Custom Workflows, Priority Support rows
    - _Requirements: 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 7.10, 7.11_

  - [ ]* 4.12 Write property test for plan card button labels
    - **Property 5: Plan card button label determined by tier comparison**
    - **Validates: Requirements 7.9, 7.10**

- [x] 5. Checkpoint — Verify components compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Pages — BillingPage and PlanComparisonPage
  - [x] 6.1 Create `BillingPage` at `src/frontend/src/features/billing/pages/BillingPage.tsx`
    - Fetch `getCurrentSubscription()` on mount, manage loading/error/data states
    - Render SubscriptionOverview, PlanDetailsCard, UsageDashboard (UsageMeter for each metric), and SubscriptionActions
    - Handle `SUBSCRIPTION_NOT_FOUND` as empty state with "Choose a Plan" CTA
    - Handle network/unexpected errors with error state + "Retry" button
    - Show SkeletonLoader while loading
    - Use responsive layout: sections stack vertically on narrow viewports, grid on wider
    - _Requirements: 4.1, 4.2, 4.6, 4.7, 5.1, 13.4_

  - [x] 6.2 Create `PlanComparisonPage` at `src/frontend/src/features/billing/pages/PlanComparisonPage.tsx`
    - Fetch `getPlans()` and `getCurrentSubscription()` in parallel on mount
    - Render plans in responsive grid (`grid-cols-1 md:grid-cols-2 lg:grid-cols-4`) ordered by `tierLevel` ascending
    - Handle upgrade: ConfirmDialog → `billingApi.upgradeSubscription()` → success toast → navigate to `/billing`
    - Handle downgrade: ConfirmDialog (destructive) → `billingApi.downgradeSubscription()` → success toast → navigate to `/billing`
    - Handle new subscription: `billingApi.createSubscription()` → success toast → navigate to `/billing`
    - Map all action errors to error toasts via `mapErrorCode`
    - Disable confirm buttons + show loading during mutations
    - Show SkeletonLoader while loading
    - Gracefully degrade if `getCurrentSubscription()` fails (show "Select Plan" on all cards)
    - _Requirements: 7.1, 7.2, 7.3, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 10.1, 10.2, 10.3, 10.4, 10.5, 13.1, 13.2, 13.3_

  - [ ]* 6.3 Write property test for plan rendering order
    - **Property 4: Plans rendered in ascending tier order**
    - **Validates: Requirements 7.3**

- [x] 7. Final checkpoint — Build verification
  - Run `npm run build` from `src/frontend/` to ensure the full project compiles with zero errors
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- The design uses TypeScript throughout — all code examples and implementations use TypeScript + React
