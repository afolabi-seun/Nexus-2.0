# Requirements Document

## Introduction

This feature extends the Nexus 2.0 platform to give the PlatformAdmin role cross-organization billing management capabilities. Currently, billing is self-service and scoped to individual organizations (OrgAdmin manages their own subscription via the BillingService). PlatformAdmin can create organizations and provision admins through ProfileService, but has no visibility into or control over billing across the platform.

This feature adds a new set of platform-level billing administration endpoints to the BillingService and a corresponding admin UI section, enabling PlatformAdmin to view billing status across all organizations, manage plans, assign or override subscriptions, and monitor usage platform-wide.

## Glossary

- **PlatformAdmin**: A system-level administrator role with no organization scope. Authenticated via the SecurityService with `roleName=PlatformAdmin` and `organizationId=Guid.Empty`. Manages the platform at a global level.
- **BillingService**: The microservice (port 5300) responsible for subscriptions, plans, feature gates, and usage tracking. Uses Clean Architecture with Domain, Application, Infrastructure, and Api layers.
- **Billing_Dashboard**: The frontend page under `/admin/billing` within the AdminLayout that displays a cross-organization overview of billing status, subscriptions, and usage.
- **Plan**: A subscription tier (Free, Starter, Professional, Enterprise) with defined limits for team members, departments, stories per month, and pricing.
- **Subscription**: An organization's active billing relationship with a specific Plan, including status (Active, Trialing, PastDue, Cancelled, Expired), billing period, and optional Stripe integration.
- **Usage_Record**: A tracked metric (active_members, stories_created, storage_bytes) for an organization within a billing period.
- **Admin_Billing_API**: New REST endpoints on the BillingService under `/api/v1/admin/billing/*` that require PlatformAdmin authentication and operate across organization boundaries (no OrganizationScope middleware).
- **PlatformAdminAttribute**: A custom authorization attribute that restricts endpoint access to users with the PlatformAdmin role. Already exists in SecurityService and ProfileService; needs to be added to BillingService.
- **ApiResponse\<T\>**: The standardized JSON envelope used by all Nexus 2.0 services, containing `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`, and `Errors`.
- **Subscription_Override**: A PlatformAdmin action that directly assigns or changes an organization's subscription plan, bypassing the normal self-service upgrade/downgrade flow and Stripe payment requirements.

## Requirements

### Requirement 1: Cross-Organization Subscription Listing

**User Story:** As a PlatformAdmin, I want to view all organization subscriptions in a single list so that I can monitor billing status across the entire platform.

#### Acceptance Criteria

1. WHEN the PlatformAdmin sends a GET request to `/api/v1/admin/billing/subscriptions`, THE Admin_Billing_API SHALL return a paginated list of all subscriptions across all organizations, each including organization name, plan name, subscription status, current period dates, and trial end date.
2. WHEN the PlatformAdmin provides a `status` query parameter, THE Admin_Billing_API SHALL filter the results to only include subscriptions matching that status (Active, Trialing, PastDue, Cancelled, Expired).
3. WHEN the PlatformAdmin provides a `search` query parameter, THE Admin_Billing_API SHALL filter results by organization name using case-insensitive partial matching.
4. IF a request to `/api/v1/admin/billing/subscriptions` is made by a user without the PlatformAdmin role, THEN THE Admin_Billing_API SHALL return HTTP 403 with error code `INSUFFICIENT_PERMISSIONS`.

### Requirement 2: Single Organization Billing Detail

**User Story:** As a PlatformAdmin, I want to view the complete billing details for a specific organization so that I can investigate billing issues or answer support inquiries.

#### Acceptance Criteria

1. WHEN the PlatformAdmin sends a GET request to `/api/v1/admin/billing/organizations/{organizationId}`, THE Admin_Billing_API SHALL return the organization's current subscription details, plan information, usage metrics for the current billing period, and billing history.
2. IF the specified organizationId does not have a subscription, THEN THE Admin_Billing_API SHALL return HTTP 404 with error code `SUBSCRIPTION_NOT_FOUND`.
3. THE Admin_Billing_API SHALL include the organization's current usage for all tracked metrics (active_members, stories_created, storage_bytes) alongside the plan limits for comparison.

### Requirement 3: Platform Admin Subscription Override

**User Story:** As a PlatformAdmin, I want to assign or change an organization's subscription plan directly so that I can handle special arrangements, trials, or support escalations without requiring the organization to self-service.

#### Acceptance Criteria

1. WHEN the PlatformAdmin sends a POST request to `/api/v1/admin/billing/organizations/{organizationId}/override` with a target planId, THE Admin_Billing_API SHALL create or update the organization's subscription to the specified plan, set the status to Active, and set the billing period to start immediately.
2. WHEN the override request includes an optional `reason` field, THE Admin_Billing_API SHALL store the reason as an audit note associated with the subscription change.
3. IF the specified planId does not exist or is inactive, THEN THE Admin_Billing_API SHALL return HTTP 404 with error code `PLAN_NOT_FOUND`.
4. WHEN a subscription override is performed, THE Admin_Billing_API SHALL publish an audit event to `outbox:billing` containing the PlatformAdmin ID, organization ID, old plan, new plan, and reason.
5. THE Admin_Billing_API SHALL allow overrides regardless of the organization's current usage levels, bypassing the normal usage-exceeds-plan-limits validation.

### Requirement 4: Platform Admin Subscription Cancellation

**User Story:** As a PlatformAdmin, I want to cancel an organization's subscription so that I can handle account terminations or policy violations at the platform level.

#### Acceptance Criteria

1. WHEN the PlatformAdmin sends a POST request to `/api/v1/admin/billing/organizations/{organizationId}/cancel`, THE Admin_Billing_API SHALL cancel the organization's subscription immediately (not at period end) and set the status to Cancelled.
2. IF the organization does not have an active or trialing subscription, THEN THE Admin_Billing_API SHALL return HTTP 400 with error code `NO_ACTIVE_SUBSCRIPTION`.
3. WHEN a PlatformAdmin cancellation is performed, THE Admin_Billing_API SHALL publish an audit event to `outbox:billing` containing the PlatformAdmin ID, organization ID, and cancellation reason.
4. WHEN the cancelled subscription has an external Stripe subscription ID, THE Admin_Billing_API SHALL cancel the Stripe subscription via the payment provider.

### Requirement 5: Plan Management — Create Plan

**User Story:** As a PlatformAdmin, I want to create new subscription plans so that I can introduce new pricing tiers or special plans for specific customer segments.

#### Acceptance Criteria

1. WHEN the PlatformAdmin sends a POST request to `/api/v1/admin/billing/plans` with plan details (name, code, tier level, limits, pricing), THE Admin_Billing_API SHALL create a new Plan entity and return it with HTTP 201.
2. IF a plan with the same planCode already exists, THEN THE Admin_Billing_API SHALL return HTTP 409 with error code `PLAN_ALREADY_EXISTS`.
3. THE Admin_Billing_API SHALL validate that tierLevel is a positive integer, maxTeamMembers is a positive integer, maxDepartments is a positive integer, maxStoriesPerMonth is a positive integer, and priceMonthly and priceYearly are non-negative decimals.

### Requirement 6: Plan Management — Update Plan

**User Story:** As a PlatformAdmin, I want to update existing subscription plans so that I can adjust pricing, limits, or features as the platform evolves.

#### Acceptance Criteria

1. WHEN the PlatformAdmin sends a PUT request to `/api/v1/admin/billing/plans/{planId}` with updated plan details, THE Admin_Billing_API SHALL update the Plan entity and return the updated plan.
2. IF the specified planId does not exist, THEN THE Admin_Billing_API SHALL return HTTP 404 with error code `PLAN_NOT_FOUND`.
3. WHEN a plan is updated, THE Admin_Billing_API SHALL publish an audit event to `outbox:billing` containing the PlatformAdmin ID, plan ID, and changed fields.
4. THE Admin_Billing_API SHALL prevent changing the planCode of an existing plan to maintain referential integrity.

### Requirement 7: Plan Management — Deactivate Plan

**User Story:** As a PlatformAdmin, I want to deactivate a subscription plan so that new organizations cannot subscribe to it while existing subscribers remain unaffected.

#### Acceptance Criteria

1. WHEN the PlatformAdmin sends a PATCH request to `/api/v1/admin/billing/plans/{planId}/deactivate`, THE Admin_Billing_API SHALL set the plan's IsActive flag to false.
2. IF the specified planId does not exist, THEN THE Admin_Billing_API SHALL return HTTP 404 with error code `PLAN_NOT_FOUND`.
3. WHILE a plan is deactivated, THE BillingService SHALL exclude the plan from the public `GET /api/v1/plans` endpoint results.
4. WHILE a plan is deactivated, THE BillingService SHALL continue to honor existing subscriptions on that plan without interruption.

### Requirement 8: Platform-Wide Usage Summary

**User Story:** As a PlatformAdmin, I want to view aggregated usage metrics across all organizations so that I can monitor platform capacity and identify organizations approaching their limits.

#### Acceptance Criteria

1. WHEN the PlatformAdmin sends a GET request to `/api/v1/admin/billing/usage/summary`, THE Admin_Billing_API SHALL return aggregated usage metrics across all organizations, including total active members, total stories created, and total storage used.
2. THE Admin_Billing_API SHALL include a breakdown of organizations grouped by their subscription plan tier.
3. WHEN the PlatformAdmin sends a GET request to `/api/v1/admin/billing/usage/organizations`, THE Admin_Billing_API SHALL return a paginated list of per-organization usage with each organization's current usage, plan limits, and utilization percentage for each metric.
4. WHEN the PlatformAdmin provides a `threshold` query parameter (0-100), THE Admin_Billing_API SHALL filter the per-organization usage list to only include organizations where at least one metric exceeds the specified utilization percentage.

### Requirement 9: PlatformAdmin Authentication for BillingService

**User Story:** As the platform, I want the BillingService to enforce PlatformAdmin role authorization on admin billing endpoints so that only authorized platform administrators can access cross-organization billing data.

#### Acceptance Criteria

1. THE BillingService SHALL include a PlatformAdminAttribute that restricts endpoint access to users with the PlatformAdmin role claim in their JWT.
2. WHEN the RoleAuthorizationMiddleware in BillingService encounters an endpoint decorated with PlatformAdminAttribute, THE BillingService SHALL verify that the JWT `roleName` claim equals `PlatformAdmin`.
3. IF a non-PlatformAdmin user attempts to access an admin billing endpoint, THEN THE BillingService SHALL return HTTP 403 with error code `INSUFFICIENT_PERMISSIONS`.
4. THE Admin_Billing_API endpoints SHALL bypass the OrganizationScopeMiddleware since PlatformAdmin tokens have `organizationId=Guid.Empty`.

### Requirement 10: Admin Billing Dashboard — Frontend

**User Story:** As a PlatformAdmin, I want a billing management section in the admin panel so that I can manage billing through a visual interface instead of API calls.

#### Acceptance Criteria

1. THE Billing_Dashboard SHALL be accessible at the route `/admin/billing` within the AdminLayout and appear as a navigation item in the admin sidebar.
2. THE Billing_Dashboard SHALL display a summary card showing total organizations by subscription status (Active, Trialing, PastDue, Cancelled, Expired).
3. THE Billing_Dashboard SHALL display a searchable, sortable table of all organization subscriptions with columns for organization name, plan, status, current period end, and usage indicators.
4. WHEN the PlatformAdmin clicks on an organization row in the subscriptions table, THE Billing_Dashboard SHALL navigate to a detail view at `/admin/billing/organizations/{organizationId}` showing full billing details and usage metrics.
5. WHEN the PlatformAdmin clicks the "Override Plan" action for an organization, THE Billing_Dashboard SHALL display a modal allowing plan selection and an optional reason field, then submit the override request to the Admin_Billing_API.
6. WHEN the PlatformAdmin clicks the "Cancel Subscription" action for an organization, THE Billing_Dashboard SHALL display a confirmation dialog with a required reason field before submitting the cancellation request.

### Requirement 11: Admin Plan Management — Frontend

**User Story:** As a PlatformAdmin, I want a plan management section in the admin panel so that I can create, edit, and deactivate subscription plans through the UI.

#### Acceptance Criteria

1. THE Billing_Dashboard SHALL include a "Plans" tab or sub-route at `/admin/billing/plans` displaying all plans (active and inactive) in a table with columns for name, code, tier, pricing, limits, and status.
2. WHEN the PlatformAdmin clicks "Create Plan", THE Billing_Dashboard SHALL display a form with fields for plan name, plan code, tier level, max team members, max departments, max stories per month, monthly price, yearly price, and features JSON.
3. WHEN the PlatformAdmin clicks "Edit" on an existing plan, THE Billing_Dashboard SHALL display a pre-populated form with the plan's current values, with the plan code field disabled.
4. WHEN the PlatformAdmin clicks "Deactivate" on an active plan, THE Billing_Dashboard SHALL display a confirmation dialog before submitting the deactivation request.
5. IF a plan creation or update fails due to validation errors, THEN THE Billing_Dashboard SHALL display the specific error messages returned by the Admin_Billing_API.
