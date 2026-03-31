# Implementation Plan: Platform Admin Billing Management

## Overview

Implement cross-organization billing management for PlatformAdmin users. The backend adds `AdminBillingController` and `AdminPlanController` to the BillingService with `PlatformAdminAttribute` authorization. The frontend adds admin billing pages under `/admin/billing` within the existing `AdminLayout`. No database migrations needed — all operations use existing `Plan`, `Subscription`, and `UsageRecord` entities with `IgnoreQueryFilters()` for cross-org access.

## Tasks

- [ ] 1. Backend foundation — PlatformAdmin authorization and domain interfaces
  - [x] 1.1 Create `PlatformAdminAttribute` and update `RoleAuthorizationMiddleware`
    - Create `BillingService.Api/Attributes/PlatformAdminAttribute.cs` mirroring the existing `OrgAdminAttribute` pattern
    - Update `RoleAuthorizationMiddleware.cs` to check for `PlatformAdminAttribute` on endpoints; when present, verify `roleName == "PlatformAdmin"` and return 403 `INSUFFICIENT_PERMISSIONS` if not
    - Ensure PlatformAdmin requests bypass the `OrgAdmin` check (PlatformAdmin tokens have `organizationId=Guid.Empty`)
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 1.2 Write property test for PlatformAdmin authorization gate
    - **Property 1: PlatformAdmin authorization gate**
    - Generate random non-PlatformAdmin role strings, mock endpoint with `PlatformAdminAttribute`, assert middleware returns 403
    - **Validates: Requirements 1.4, 9.2, 9.3**

  - [x] 1.3 Add new error codes and domain service interfaces
    - Add `PLAN_ALREADY_EXISTS` (5015), `INSUFFICIENT_PERMISSIONS` (5016), `PLAN_CODE_IMMUTABLE` (5017) to `ErrorCodes.cs`
    - Create `IAdminBillingService` interface in `BillingService.Domain/Interfaces/Services/`
    - Create `IAdminPlanService` interface in `BillingService.Domain/Interfaces/Services/`
    - Extend `IPlanRepository` with `GetAllAsync(CancellationToken)` and `UpdateAsync(Plan, CancellationToken)`
    - Extend `ISubscriptionRepository` with `GetAllWithPlansAsync(CancellationToken)` and `GetCountByStatusAsync(string, CancellationToken)`
    - Extend `IUsageRecordRepository` with `GetAllCurrentPeriodAsync(DateTime, CancellationToken)`
    - _Requirements: 1.1, 5.1, 6.1, 8.1_

  - [x] 1.4 Create admin DTOs and FluentValidation validators
    - Create `BillingService.Application/DTOs/Admin/` subfolder with all admin DTOs: `AdminSubscriptionListItem`, `PaginatedResponse<T>`, `AdminOrganizationBillingResponse`, `AdminOverrideRequest`, `AdminCancelRequest`, `AdminUsageSummaryResponse`, `PlanTierBreakdown`, `AdminOrganizationUsageItem`, `UsageMetricWithLimit`, `AdminCreatePlanRequest`, `AdminUpdatePlanRequest`, `AdminPlanResponse`
    - Create `AdminCreatePlanRequestValidator` in `Validators/Admin/` — positive integers for tier/members/departments/stories, non-negative decimals for pricing, non-empty strings, planCode format `^[A-Z0-9_]{2,20}$`
    - Create `AdminUpdatePlanRequestValidator` — same validations minus planCode
    - Create `AdminOverrideRequestValidator` — validates planId is not empty Guid
    - _Requirements: 5.1, 5.3, 6.1, 3.1_

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 3. Backend services — AdminBillingService implementation
  - [x] 3.1 Implement `AdminBillingService` in `Infrastructure/Services/AdminBilling/`
    - Implement `GetAllSubscriptionsAsync` — use `_dbContext.Subscriptions.IgnoreQueryFilters().Include(s => s.Plan)` with optional status filter, search filter (case-insensitive partial match on org name via ProfileService), and pagination
    - Implement `GetOrganizationBillingAsync` — return subscription details, plan info, current usage metrics with plan limits; throw `SUBSCRIPTION_NOT_FOUND` if no subscription exists
    - Implement `OverrideSubscriptionAsync` — create or update subscription to target plan, set status to Active, set `CurrentPeriodStart` to `DateTime.UtcNow`, bypass usage limit checks, publish audit event to `outbox:billing`, throw `PLAN_NOT_FOUND` for invalid planId
    - Implement `AdminCancelSubscriptionAsync` — set status to Cancelled immediately (not at period end), set `CancelledAt` to `DateTime.UtcNow`, cancel Stripe subscription if `ExternalSubscriptionId` exists, publish audit event, throw `NO_ACTIVE_SUBSCRIPTION` if not Active/Trialing
    - Implement `GetUsageSummaryAsync` — aggregate usage records across all orgs using `IgnoreQueryFilters()`, group by metric, include plan tier breakdown
    - Implement `GetOrganizationUsageListAsync` — per-org usage with plan limits, calculate `percentUsed`, filter by threshold if provided, paginate
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 2.3, 3.1, 3.2, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 8.1, 8.2, 8.3, 8.4_

  - [ ] 3.2 Write property test for status filter correctness
    - **Property 2: Status filter correctness**
    - Generate random subscription lists with mixed statuses, apply each valid status filter, assert all results match the filter and no matching subscriptions are omitted
    - **Validates: Requirements 1.2**

  - [ ] 3.3 Write property test for search filter case-insensitive partial match
    - **Property 3: Search filter case-insensitive partial match**
    - Generate random org names and search substrings, assert all results contain the search term case-insensitively
    - **Validates: Requirements 1.3**

  - [ ] 3.4 Write property test for paginated response completeness
    - **Property 4: Paginated response completeness**
    - Generate random N subscriptions and random page/pageSize, assert `totalCount = N`, correct item count per page, and full iteration yields all N with no duplicates
    - **Validates: Requirements 1.1**

  - [x] 3.5 Write property test for override sets Active status and current period
    - **Property 6: Override sets Active status and current period**
    - Generate random orgs and plans, perform override, assert `status = "Active"`, correct `planId`, and `currentPeriodStart` within delta of UTC now
    - **Validates: Requirements 3.1**

  - [ ] 3.6 Write property test for override bypasses usage limits
    - **Property 7: Override bypasses usage limits**
    - Generate orgs with usage exceeding target plan limits, assert override succeeds (HTTP 200) and subscription is updated
    - **Validates: Requirements 3.5**

  - [ ] 3.7 Write property test for admin mutation audit events
    - **Property 8: Admin mutation audit events contain required fields**
    - For each admin mutation type, assert outbox message contains non-empty `adminId`, `entityId`, `action`, and `reason` when provided
    - **Validates: Requirements 3.2, 3.4, 4.3, 6.3**

  - [x] 3.8 Write property test for admin cancellation is immediate
    - **Property 9: Admin cancellation is immediate**
    - Generate orgs with Active/Trialing subscriptions, perform admin cancel, assert `status = "Cancelled"` and `cancelledAt` within delta of UTC now
    - **Validates: Requirements 4.1**

  - [ ] 3.9 Write property test for usage summary aggregation correctness
    - **Property 15: Usage summary aggregation correctness**
    - Generate random usage records across orgs, assert totals equal sums and `byPlanTier` counts match subscription counts per plan
    - **Validates: Requirements 8.1, 8.2**

  - [ ] 3.10 Write property test for usage threshold filter correctness
    - **Property 16: Usage threshold filter correctness**
    - Generate random per-org usage with random threshold, assert every org in results has at least one metric >= threshold, and no qualifying org is omitted
    - **Validates: Requirements 8.4**

  - [ ] 3.11 Write property test for utilization percentage calculation
    - **Property 17: Utilization percentage calculation**
    - Generate random usage/limit pairs with positive limits, assert `percentUsed = (currentValue / limit) * 100`
    - **Validates: Requirements 8.3**

- [ ] 4. Backend services — AdminPlanService implementation
  - [x] 4.1 Implement `AdminPlanService` in `Infrastructure/Services/AdminBilling/`
    - Implement `GetAllPlansAsync` — return all plans including inactive using `IgnoreQueryFilters()` or unfiltered query (plans have no org scope)
    - Implement `CreatePlanAsync` — check for duplicate planCode (throw `PLAN_ALREADY_EXISTS`), create Plan entity, return with HTTP 201
    - Implement `UpdatePlanAsync` — validate planId exists (throw `PLAN_NOT_FOUND`), reject planCode changes (throw `PLAN_CODE_IMMUTABLE`), update entity, publish audit event
    - Implement `DeactivatePlanAsync` — validate planId exists, set `IsActive = false`, publish audit event
    - _Requirements: 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2_

  - [ ] 4.2 Write property test for plan creation round trip
    - **Property 10: Plan creation round trip**
    - Generate random valid plan requests with unique planCodes, create then retrieve, assert all fields match
    - **Validates: Requirements 5.1**

  - [ ] 4.3 Write property test for plan validation rejects invalid inputs
    - **Property 11: Plan validation rejects invalid inputs**
    - Generate plan requests with at least one invalid field (non-positive integers, negative decimals), assert validation error returned and no plan created
    - **Validates: Requirements 5.3**

  - [ ] 4.4 Write property test for plan update preserves planCode immutability
    - **Property 12: Plan update preserves planCode immutability**
    - Generate existing plans, update via `AdminUpdatePlanRequest`, assert returned plan's `planCode` equals original
    - **Validates: Requirements 6.4, 6.1**

- [ ] 5. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Backend controllers — AdminBillingController and AdminPlanController
  - [x] 6.1 Create `AdminBillingController` in `BillingService.Api/Controllers/`
    - Route base: `api/v1/admin/billing`
    - Decorate class with `[PlatformAdmin]`, `[Authorize]`, `[ApiController]`
    - `GET /subscriptions` — accepts `status`, `search`, `page`, `pageSize` query params, delegates to `IAdminBillingService.GetAllSubscriptionsAsync`
    - `GET /organizations/{organizationId}` — delegates to `GetOrganizationBillingAsync`
    - `POST /organizations/{organizationId}/override` — accepts `AdminOverrideRequest` body, extracts admin ID from JWT claims, delegates to `OverrideSubscriptionAsync`
    - `POST /organizations/{organizationId}/cancel` — accepts `AdminCancelRequest` body, delegates to `AdminCancelSubscriptionAsync`
    - `GET /usage/summary` — delegates to `GetUsageSummaryAsync`
    - `GET /usage/organizations` — accepts `threshold`, `page`, `pageSize` query params, delegates to `GetOrganizationUsageListAsync`
    - Wrap all responses in `ApiResponse<T>` with `CorrelationId`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 8.1, 8.2, 8.3, 8.4_

  - [x] 6.2 Create `AdminPlanController` in `BillingService.Api/Controllers/`
    - Route base: `api/v1/admin/billing/plans`
    - Decorate class with `[PlatformAdmin]`, `[Authorize]`, `[ApiController]`
    - `GET /` — delegates to `IAdminPlanService.GetAllPlansAsync`
    - `POST /` — accepts `AdminCreatePlanRequest`, validates, delegates to `CreatePlanAsync`, returns 201
    - `PUT /{planId}` — accepts `AdminUpdatePlanRequest`, delegates to `UpdatePlanAsync`
    - `PATCH /{planId}/deactivate` — delegates to `DeactivatePlanAsync`
    - Wrap all responses in `ApiResponse<T>` with `CorrelationId`
    - _Requirements: 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2_

  - [x] 6.3 Register admin services in DI container
    - Register `IAdminBillingService` → `AdminBillingService` and `IAdminPlanService` → `AdminPlanService` in `Program.cs` or `ControllerServiceExtensions.cs`
    - Register new validators in the FluentValidation pipeline
    - _Requirements: 9.1_

  - [ ] 6.4 Write property test for deactivated plans excluded from public listing
    - **Property 13: Deactivated plans excluded from public listing**
    - Deactivate a plan, assert public `GET /api/v1/plans` excludes it while admin `GET /api/v1/admin/billing/plans` includes it
    - **Validates: Requirements 7.3**

  - [ ] 6.5 Write property test for deactivated plans do not disrupt existing subscriptions
    - **Property 14: Deactivated plans do not disrupt existing subscriptions**
    - Create subscription on a plan, deactivate the plan, assert subscription details still return full plan info and status is unchanged
    - **Validates: Requirements 7.4**

  - [ ] 6.6 Write property test for usage metrics include plan limits
    - **Property 5: Usage metrics include plan limits**
    - Generate org with subscription and usage records, assert billing detail includes all tracked metrics with both `currentValue` and `limit`
    - **Validates: Requirements 2.3**

- [ ] 7. Checkpoint — Ensure all backend tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Frontend foundation — types, API client, and store
  - [x] 8.1 Create admin billing TypeScript types
    - Create `src/frontend/src/types/adminBilling.ts` with all interfaces: `AdminSubscriptionListItem`, `PaginatedResponse<T>`, `AdminUsageSummary`, `PlanTierBreakdown`, `AdminOrganizationUsageItem`, `UsageMetricWithLimit`, `AdminOverrideRequest`, `AdminCancelRequest`, `AdminPlanResponse`, `AdminCreatePlanRequest`, `AdminUpdatePlanRequest`
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 8.1_

  - [x] 8.2 Create admin billing API client
    - Create `src/frontend/src/api/adminBillingApi.ts` using the existing `createApiClient` pattern from `billingApi.ts`
    - Implement all admin endpoint methods: `getSubscriptions`, `getOrganizationBilling`, `overrideSubscription`, `cancelSubscription`, `getUsageSummary`, `getOrganizationUsage`, `getPlans`, `createPlan`, `updatePlan`, `deactivatePlan`
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 8.1_

  - [x] 8.3 Create admin billing Zustand store
    - Create `src/frontend/src/stores/adminBillingStore.ts` following the existing store pattern (`authStore.ts`, `orgStore.ts`)
    - Manage state for: subscriptions list, plans list, usage summary, selected organization billing, loading/error states, pagination, filters (status, search, threshold)
    - Implement actions that call the API client and update state
    - _Requirements: 10.2, 10.3, 11.1_

- [ ] 9. Frontend pages — Admin Billing Dashboard
  - [x] 9.1 Create `PlatformAdminBillingPage` at `src/frontend/src/features/admin/pages/`
    - Display summary cards showing total organizations by subscription status (Active, Trialing, PastDue, Cancelled, Expired)
    - Display searchable, sortable subscriptions table using the shared `DataTable` component with columns: organization name, plan, status, current period end, usage indicators
    - Add status filter dropdown and search input
    - Row click navigates to `/admin/billing/organizations/{organizationId}`
    - Add "Override Plan" and "Cancel Subscription" action buttons per row
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

  - [x] 9.2 Create `PlatformAdminOrgBillingDetailPage` at `src/frontend/src/features/admin/pages/`
    - Display full billing details: subscription info, plan details, usage metrics with plan limits comparison
    - Show usage meters for each tracked metric (active_members, stories_created, storage_bytes)
    - Include "Override Plan" and "Cancel Subscription" actions
    - _Requirements: 2.1, 2.2, 2.3, 10.4_

  - [x] 9.3 Create override and cancellation modals
    - `OverridePlanModal` — plan selection dropdown (from plans list), optional reason textarea, submit button
    - `CancelSubscriptionModal` — confirmation dialog with required reason textarea, submit button
    - Both modals show loading state during API call and display error messages on failure
    - _Requirements: 10.5, 10.6_

  - [ ] 9.4 Write property test for summary card status counts
    - **Property 18: Summary card status counts**
    - Generate random subscription arrays with random statuses, render summary component, assert displayed counts match actual counts per status and sum equals total
    - **Validates: Requirements 10.2**

- [ ] 10. Frontend pages — Admin Plan Management
  - [x] 10.1 Create `PlatformAdminPlansPage` at `src/frontend/src/features/admin/pages/`
    - Display all plans (active and inactive) in a table with columns: name, code, tier, monthly price, yearly price, limits, status (active/inactive badge)
    - "Create Plan" button opens the plan form modal
    - "Edit" action per row opens pre-populated form modal with planCode field disabled
    - "Deactivate" action per row shows confirmation dialog
    - Display validation error messages from API on form submission failure
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

  - [x] 10.2 Create plan form modal component
    - Fields: plan name, plan code (disabled on edit), tier level, max team members, max departments, max stories per month, monthly price, yearly price, features JSON
    - Client-side validation matching backend validators
    - Handles both create and edit modes
    - _Requirements: 11.2, 11.3, 11.5_

- [ ] 11. Frontend wiring — routes, sidebar, and integration
  - [x] 11.1 Update `AdminLayout` sidebar with Billing nav item
    - Add "Billing" `NavLink` to `/admin/billing` using `CreditCard` icon from lucide-react, following the existing Organizations nav item pattern
    - _Requirements: 10.1_

  - [x] 11.2 Update `router.tsx` with admin billing routes
    - Add routes under the PlatformAdmin `RoleGuard` section within `AdminLayout`:
      - `/admin/billing` → `PlatformAdminBillingPage`
      - `/admin/billing/organizations/:id` → `PlatformAdminOrgBillingDetailPage`
      - `/admin/billing/plans` → `PlatformAdminPlansPage`
    - _Requirements: 10.1, 10.4, 11.1_

- [x] 12. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document (18 properties total)
- Backend uses FsCheck + xUnit; frontend uses fast-check + Vitest
- All admin endpoints use `IgnoreQueryFilters()` for cross-organization access
- No database migrations needed — operates on existing Plan, Subscription, and UsageRecord entities
