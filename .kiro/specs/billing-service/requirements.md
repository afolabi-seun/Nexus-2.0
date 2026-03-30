# Requirements Document — BillingService

## Introduction

This document defines the complete requirements for the Nexus-2.0 BillingService — the 5th microservice in the Enterprise Agile Platform. BillingService runs on port 5300 with database `nexus_billing` and follows Clean Architecture (.NET 8) with Domain / Application / Infrastructure / Api layers.

BillingService handles subscription management, plan tiers, feature gating, usage tracking, trial management, and payment integration (Stripe) for the Nexus-2.0 platform. It enables a SaaS model where organizations subscribe to different plan tiers (Free, Starter, Professional, Enterprise) with varying feature access and capacity limits.

BillingService depends on ProfileService for organization lookup and plan tier propagation (updating `OrganizationSettings.PlanTier`). It depends on SecurityService for service-to-service JWT issuance. It publishes billing audit events to `outbox:billing` for UtilityService processing. Other services (SecurityService, WorkService, ProfileService) consume plan tier data from Redis cache (`plan:{organizationId}`) or call BillingService's feature-gate endpoint on cache miss.

All requirements are derived from:
- `docs/nexus-2.0-backend-requirements.md` (Appendix C: Future — BillingService)
- `docs/nexus-2.0-backend-specification.md` (Cross-cutting patterns, sections 2–3, 8)
- Existing service patterns from SecurityService, ProfileService, WorkService, and UtilityService

## Glossary

- **BillingService**: Microservice (port 5300, database `nexus_billing`) responsible for subscription management, plan tiers, feature gating, usage tracking, trial management, and payment integration.
- **ProfileService**: Microservice (port 5002) that owns Organization and TeamMember records. BillingService calls ProfileService to update `OrganizationSettings.PlanTier` on subscription changes and to resolve organization details.
- **SecurityService**: Microservice (port 5001) responsible for authentication, JWT issuance, and RBAC. BillingService calls SecurityService for service-to-service JWT issuance. SecurityService reads plan tier from Redis cache for `FeatureGateMiddleware`.
- **WorkService**: Microservice (port 5003) responsible for stories, tasks, sprints, and boards. WorkService reads plan tier from Redis cache to enforce feature limits (max stories per month, sprint analytics access, custom workflows).
- **UtilityService**: Microservice (port 5200) responsible for audit logging, notifications, and reference data. BillingService publishes billing events to `outbox:billing` for UtilityService processing.
- **Subscription**: Entity representing an organization's billing relationship. One subscription per organization. Tracks plan, status, billing period, trial dates, and Stripe external ID.
- **Plan**: Entity defining a billing tier with feature limits and pricing. Four seeded tiers: Free, Starter, Professional, Enterprise.
- **UsageRecord**: Entity tracking organization usage metrics (active members, stories created, storage bytes) per billing period for limit enforcement and billing.
- **Plan_Tier**: One of four levels — Free, Starter, Professional, Enterprise — each with distinct feature limits and pricing.
- **Feature_Gate**: A check that enforces plan-tier limits on specific features (e.g., max team members, max departments, sprint analytics access, custom workflows).
- **Stripe**: External payment processor used for subscription billing, invoicing, and webhook-driven event handling.
- **Stripe_Webhook**: HTTP POST callback from Stripe notifying BillingService of payment events (payment succeeded, payment failed, subscription updated, subscription deleted).
- **Trial**: A time-limited free access period (default 14 days) for a paid plan tier, after which the subscription converts to active billing or expires to the Free tier.
- **Subscription_Status**: Lifecycle state of a subscription — `Active`, `Trialing`, `PastDue`, `Cancelled`, `Expired`.
- **FeaturesJson**: JSON column on the Plan entity storing feature flags as key-value pairs (e.g., `{"sprintAnalytics": "full", "customWorkflows": true, "prioritySupport": true}`).
- **Organization**: Top-level tenant entity owned by ProfileService. BillingService references organizations by `OrganizationId` and maintains one subscription per organization.
- **ApiResponse**: Standardized JSON envelope `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`, `Errors` fields.
- **DomainException**: Base exception class for business rule violations, containing `ErrorValue`, `ErrorCode`, `StatusCode`, and `CorrelationId`.
- **CorrelationId**: End-to-end trace identifier (`X-Correlation-Id` header) propagated across all service calls and included in all API responses.
- **Outbox**: Redis-based async messaging pattern. BillingService publishes audit events to `outbox:billing`. UtilityService polls and processes the queue.
- **IOrganizationEntity**: Marker interface for entities scoped to an organization, enabling EF Core global query filters by `OrganizationId`.
- **Polly**: .NET resilience library used for retry (3x exponential), circuit breaker (5 failures / 30s), and timeout (10s) on inter-service calls.
- **Clean_Architecture**: Four-layer architecture — Domain (entities, interfaces), Application (DTOs, validators), Infrastructure (EF Core, Redis, HTTP clients), Api (controllers, middleware).
- **Service_JWT**: Short-lived JWT for inter-service communication, issued by SecurityService and cached in Redis. Used by BillingService when calling ProfileService.

## Requirements

### Requirement 1: Plan Tier Management

**User Story:** As a platform administrator, I want to define and manage plan tiers so that organizations can subscribe to different levels of service with appropriate feature limits and pricing.

#### Acceptance Criteria

1. THE BillingService SHALL seed four plan tiers on database initialization: Free (`free`), Starter (`starter`), Professional (`pro`), and Enterprise (`enterprise`).
2. WHEN `GET /api/v1/plans` is called by an authenticated user, THE BillingService SHALL return all active plans with their feature limits and pricing.
3. THE BillingService SHALL enforce the following plan limits:

| Tier | MaxTeamMembers | MaxDepartments | MaxStoriesPerMonth | SprintAnalytics | CustomWorkflows | PrioritySupport |
|------|---------------|----------------|-------------------|-----------------|-----------------|-----------------|
| Free | 5 | 3 | 50 | none | false | false |
| Starter | 25 | 5 | 500 | basic | false | false |
| Professional | 100 | 0 (unlimited) | 0 (unlimited) | full | true | true |
| Enterprise | 0 (unlimited) | 0 (unlimited) | 0 (unlimited) | full | true | true |

4. WHEN plan data is requested, THE BillingService SHALL cache plan details in Redis (`plan:{planId}`, 60-minute TTL) and serve from cache on subsequent requests.
5. THE BillingService SHALL store extended feature flags in the `FeaturesJson` column as a JSON object (e.g., `{"sprintAnalytics": "full", "customWorkflows": true, "prioritySupport": true}`).
6. WHEN a plan with `PlanCode` that matches an existing plan is inserted during seeding, THE BillingService SHALL skip the duplicate without error.

### Requirement 2: Subscription Creation

**User Story:** As an OrgAdmin, I want to create a subscription for my organization so that the organization can access paid features.

#### Acceptance Criteria

1. WHEN `POST /api/v1/subscriptions` is called with `{planId, paymentMethodToken}`, THE BillingService SHALL create a subscription for the authenticated user's organization, set `Status` to `Trialing` with a 14-day trial period, and return HTTP 201 with the subscription details.
2. WHEN a subscription is created, THE BillingService SHALL validate that the organization does not already have an active or trialing subscription. IF a subscription already exists, THEN THE BillingService SHALL return HTTP 409 with `SUBSCRIPTION_ALREADY_EXISTS` (5001).
3. WHEN a subscription is created for a paid plan (Starter, Professional, Enterprise), THE BillingService SHALL create a corresponding Stripe subscription via the Stripe API and store the `ExternalSubscriptionId`.
4. WHEN a subscription is created for the Free plan, THE BillingService SHALL create the subscription locally without calling Stripe, set `Status` to `Active`, and set `CurrentPeriodEnd` to null (no expiry).
5. WHEN a subscription is created, THE BillingService SHALL update the organization's plan tier in Redis cache (`plan:{organizationId}`) and call ProfileService to update `OrganizationSettings.PlanTier`.
6. WHEN a subscription is created, THE BillingService SHALL publish a `SubscriptionCreated` audit event to `outbox:billing` with `OrganizationId`, `PlanName`, and `Status`.
7. IF the specified plan does not exist or is inactive, THEN THE BillingService SHALL return HTTP 404 with `PLAN_NOT_FOUND` (5002).
8. IF the Stripe API call fails, THEN THE BillingService SHALL return HTTP 502 with `PAYMENT_PROVIDER_ERROR` (5010) and not create the local subscription record.

### Requirement 3: Subscription Retrieval

**User Story:** As an OrgAdmin, I want to view my organization's current subscription so that I can see the plan, status, and billing period.

#### Acceptance Criteria

1. WHEN `GET /api/v1/subscriptions/current` is called by an authenticated OrgAdmin, THE BillingService SHALL return the current subscription for the user's organization, including plan details, status, billing period dates, trial end date, and usage summary.
2. IF the organization has no subscription, THEN THE BillingService SHALL return HTTP 404 with `SUBSCRIPTION_NOT_FOUND` (5003).
3. WHEN the subscription is returned, THE BillingService SHALL include the associated plan's feature limits and pricing in the response.

### Requirement 4: Subscription Upgrade

**User Story:** As an OrgAdmin, I want to upgrade my organization's plan so that the team can access more features and higher limits.

#### Acceptance Criteria

1. WHEN `PATCH /api/v1/subscriptions/upgrade` is called with `{newPlanId}`, THE BillingService SHALL validate that the new plan is a higher tier than the current plan. IF the new plan is the same tier or lower, THEN THE BillingService SHALL return HTTP 400 with `INVALID_UPGRADE_PATH` (5004).
2. WHEN a subscription is upgraded, THE BillingService SHALL update the Stripe subscription with the new plan's price and apply prorated charges for the remainder of the current billing period.
3. WHEN a subscription is upgraded, THE BillingService SHALL update the local subscription record with the new `PlanId`, refresh the Redis cache (`plan:{organizationId}`), and call ProfileService to update `OrganizationSettings.PlanTier`.
4. WHEN a subscription is upgraded, THE BillingService SHALL publish a `SubscriptionUpgraded` audit event to `outbox:billing` with `OrganizationId`, `OldPlanName`, `NewPlanName`.
5. IF the organization has no active subscription, THEN THE BillingService SHALL return HTTP 400 with `NO_ACTIVE_SUBSCRIPTION` (5005).
6. WHEN a subscription is upgraded during a trial, THE BillingService SHALL end the trial immediately and start the paid billing period.

### Requirement 5: Subscription Downgrade

**User Story:** As an OrgAdmin, I want to downgrade my organization's plan so that costs are reduced when fewer features are needed.

#### Acceptance Criteria

1. WHEN `PATCH /api/v1/subscriptions/downgrade` is called with `{newPlanId}`, THE BillingService SHALL validate that the new plan is a lower tier than the current plan. IF the new plan is the same tier or higher, THEN THE BillingService SHALL return HTTP 400 with `INVALID_DOWNGRADE_PATH` (5006).
2. WHEN a subscription is downgraded, THE BillingService SHALL schedule the downgrade to take effect at the end of the current billing period (`CurrentPeriodEnd`), not immediately.
3. WHEN a scheduled downgrade takes effect, THE BillingService SHALL update the Stripe subscription, update the local subscription record with the new `PlanId`, refresh the Redis cache (`plan:{organizationId}`), and call ProfileService to update `OrganizationSettings.PlanTier`.
4. WHEN a downgrade is scheduled, THE BillingService SHALL validate that the organization's current usage does not exceed the new plan's limits. IF usage exceeds limits (e.g., 30 active members downgrading to Starter with max 25), THEN THE BillingService SHALL return HTTP 400 with `USAGE_EXCEEDS_PLAN_LIMITS` (5007) and include the specific limits that are exceeded.
5. WHEN a subscription is downgraded, THE BillingService SHALL publish a `SubscriptionDowngraded` audit event to `outbox:billing` with `OrganizationId`, `OldPlanName`, `NewPlanName`, `EffectiveDate`.
6. IF the organization has no active subscription, THEN THE BillingService SHALL return HTTP 400 with `NO_ACTIVE_SUBSCRIPTION` (5005).

### Requirement 6: Subscription Cancellation

**User Story:** As an OrgAdmin, I want to cancel my organization's subscription so that billing stops when the service is no longer needed.

#### Acceptance Criteria

1. WHEN `POST /api/v1/subscriptions/cancel` is called, THE BillingService SHALL set the subscription `Status` to `Cancelled`, record `CancelledAt` as `DateTime.UtcNow`, and schedule the cancellation to take effect at `CurrentPeriodEnd`.
2. WHEN a cancellation takes effect at period end, THE BillingService SHALL cancel the Stripe subscription, downgrade the organization to the Free plan, refresh the Redis cache (`plan:{organizationId}`), and call ProfileService to update `OrganizationSettings.PlanTier` to `Free`.
3. WHEN a subscription is cancelled, THE BillingService SHALL publish a `SubscriptionCancelled` audit event to `outbox:billing` with `OrganizationId`, `PlanName`, `EffectiveDate`.
4. IF the subscription is already cancelled, THEN THE BillingService SHALL return HTTP 400 with `SUBSCRIPTION_ALREADY_CANCELLED` (5008).
5. IF the organization has no subscription, THEN THE BillingService SHALL return HTTP 400 with `NO_ACTIVE_SUBSCRIPTION` (5005).
6. WHEN a subscription on the Free plan is cancelled, THE BillingService SHALL set `Status` to `Cancelled` immediately without calling Stripe.

### Requirement 7: Trial Management

**User Story:** As an OrgAdmin, I want a free trial period when subscribing to a paid plan so that the team can evaluate features before committing to payment.

#### Acceptance Criteria

1. WHEN a subscription is created for a paid plan, THE BillingService SHALL set `Status` to `Trialing` and `TrialEndDate` to 14 days from creation.
2. WHEN a trial period expires, THE BillingService SHALL transition the subscription to `Active` status and begin billing via Stripe. IF no valid payment method is on file, THEN THE BillingService SHALL transition the subscription to `Expired` status and downgrade the organization to the Free plan.
3. WHILE a subscription is in `Trialing` status, THE BillingService SHALL grant full access to the subscribed plan's features without charging.
4. WHEN a trial subscription is upgraded to a different paid plan, THE BillingService SHALL end the trial immediately and start the paid billing period on the new plan.
5. THE BillingService SHALL check for expired trials periodically (via a background hosted service running every hour) and process expirations automatically.
6. WHEN a trial expires and the organization is downgraded, THE BillingService SHALL publish a `TrialExpired` audit event to `outbox:billing` with `OrganizationId`, `ExpiredPlanName`.

### Requirement 8: Feature Gating

**User Story:** As the platform, I want to enforce plan-tier limits across all services so that organizations only access features included in their subscribed plan.

#### Acceptance Criteria

1. WHEN `GET /api/v1/feature-gates/{feature}` is called with `organizationId` query parameter (Service auth), THE BillingService SHALL return whether the feature is allowed for the organization's current plan, along with the current usage and limit values.
2. THE BillingService SHALL enforce the following feature gates:

| Feature Key | Free | Starter | Professional | Enterprise |
|-------------|------|---------|--------------|------------|
| `max_team_members` | 5 | 25 | 100 | unlimited |
| `max_departments` | 3 | 5 | unlimited | unlimited |
| `max_stories_per_month` | 50 | 500 | unlimited | unlimited |
| `sprint_analytics` | none | basic | full | full |
| `custom_workflows` | false | false | true | true |
| `priority_support` | false | false | true | true |

3. WHEN a feature gate is checked, THE BillingService SHALL first read from Redis cache (`plan:{organizationId}`, 60-minute TTL). IF the cache misses, THEN THE BillingService SHALL query the database and populate the cache.
4. WHEN a plan change occurs (upgrade, downgrade, cancellation, trial expiry), THE BillingService SHALL invalidate and refresh the Redis cache (`plan:{organizationId}`) immediately.
5. WHEN a feature gate check determines that the organization has exceeded a limit, THE BillingService SHALL return `{ "allowed": false, "currentUsage": <value>, "limit": <value>, "feature": "<key>" }` with HTTP 200.
6. WHEN a feature gate check determines that the feature is within limits, THE BillingService SHALL return `{ "allowed": true, "currentUsage": <value>, "limit": <value>, "feature": "<key>" }` with HTTP 200.

### Requirement 9: Usage Tracking

**User Story:** As an OrgAdmin, I want to see my organization's usage metrics so that I can monitor consumption against plan limits and make informed decisions about plan changes.

#### Acceptance Criteria

1. WHEN `GET /api/v1/usage` is called by an authenticated OrgAdmin, THE BillingService SHALL return current usage metrics for the organization: `active_members` count, `stories_created` count for the current billing period, and `storage_bytes` used.
2. THE BillingService SHALL maintain usage counters in Redis (`usage:{organizationId}:{metric}`, 5-minute TTL) for fast reads, with periodic persistence to the `UsageRecord` table.
3. WHEN a usage metric is updated (e.g., a new team member is added, a story is created), THE BillingService SHALL expose an internal service endpoint `POST /api/v1/usage/increment` (Service auth) that increments the specified metric counter.
4. WHEN usage data is returned, THE BillingService SHALL include the plan limit for each metric alongside the current value, enabling the caller to display percentage utilization.
5. WHEN a new billing period starts, THE BillingService SHALL reset period-scoped counters (`stories_created`) and archive the previous period's `UsageRecord`.
6. IF the organization has no subscription, THEN THE BillingService SHALL return usage against Free plan limits.

### Requirement 10: Stripe Webhook Handling

**User Story:** As the platform, I want to process Stripe webhook events so that subscription status and billing records stay synchronized with the payment provider.

#### Acceptance Criteria

1. WHEN `POST /api/v1/webhooks/stripe` is called, THE BillingService SHALL verify the webhook signature using the Stripe webhook secret before processing the event. IF the signature is invalid, THEN THE BillingService SHALL return HTTP 400 with `INVALID_WEBHOOK_SIGNATURE` (5011).
2. WHEN a `invoice.payment_succeeded` event is received, THE BillingService SHALL update the subscription's `CurrentPeriodStart` and `CurrentPeriodEnd` to the new billing period and set `Status` to `Active` if it was `PastDue`.
3. WHEN a `invoice.payment_failed` event is received, THE BillingService SHALL set the subscription `Status` to `PastDue` and publish a `PaymentFailed` notification to `outbox:billing` with `OrganizationId` and `FailureReason`.
4. WHEN a `customer.subscription.updated` event is received, THE BillingService SHALL synchronize the local subscription record with the Stripe subscription state (plan, status, period dates).
5. WHEN a `customer.subscription.deleted` event is received, THE BillingService SHALL set the subscription `Status` to `Cancelled`, downgrade the organization to the Free plan, and refresh the Redis cache.
6. THE BillingService SHALL implement idempotent webhook processing by storing processed Stripe event IDs and skipping duplicate events.
7. WHEN a webhook event is processed, THE BillingService SHALL publish a `WebhookProcessed` audit event to `outbox:billing` with `EventType`, `OrganizationId`, and `StripeEventId`.
8. IF the webhook payload cannot be deserialized, THEN THE BillingService SHALL return HTTP 400 with `INVALID_WEBHOOK_PAYLOAD` (5012) and log the error.

### Requirement 11: Stripe Payment Integration

**User Story:** As the platform, I want to integrate with Stripe for payment processing so that organizations are billed correctly for their subscriptions.

#### Acceptance Criteria

1. THE BillingService SHALL use the Stripe .NET SDK to communicate with the Stripe API for subscription creation, updates, and cancellation.
2. WHEN a subscription is created, THE BillingService SHALL create a Stripe Customer (if one does not exist for the organization) and a Stripe Subscription linked to the appropriate Stripe Price.
3. WHEN a subscription is upgraded, THE BillingService SHALL update the Stripe Subscription's price item and apply prorated billing.
4. WHEN a subscription is cancelled, THE BillingService SHALL cancel the Stripe Subscription at period end (`cancel_at_period_end: true`).
5. IF the Stripe API returns an error, THEN THE BillingService SHALL wrap the error in a `DomainException` with `PAYMENT_PROVIDER_ERROR` (5010) and include the Stripe error message in the error details.
6. THE BillingService SHALL store the Stripe Customer ID on the subscription record (`ExternalCustomerId`) for subsequent API calls.
7. WHEN communicating with the Stripe API, THE BillingService SHALL use Polly resilience policies (retry 3x exponential, circuit breaker 5 failures / 30s, timeout 10s).

### Requirement 12: Cross-Service Integration

**User Story:** As the platform, I want BillingService to integrate with existing services so that plan tier changes propagate correctly and billing events are audited.

#### Acceptance Criteria

1. WHEN a plan tier changes (subscription created, upgraded, downgraded, cancelled, trial expired), THE BillingService SHALL call ProfileService `PUT /api/v1/organizations/{id}/settings` to update `OrganizationSettings.PlanTier` with the new plan code.
2. WHEN calling ProfileService, THE BillingService SHALL use a typed `IProfileServiceClient` with Polly resilience policies (retry 3x exponential, circuit breaker 5 failures / 30s, timeout 10s) and `CorrelationIdDelegatingHandler` for trace propagation.
3. WHEN calling ProfileService or SecurityService, THE BillingService SHALL authenticate using a service-to-service JWT obtained from SecurityService `POST /api/v1/service-tokens/issue`, cached in Redis (`service_token:billing`, 23-hour TTL).
4. THE BillingService SHALL publish all billing events to `outbox:billing` using the Redis outbox pattern. Event types include: `SubscriptionCreated`, `SubscriptionUpgraded`, `SubscriptionDowngraded`, `SubscriptionCancelled`, `TrialExpired`, `PaymentFailed`, `WebhookProcessed`.
5. IF ProfileService is unavailable when propagating a plan tier change, THEN THE BillingService SHALL retry via Polly policies and log the failure. The Redis cache (`plan:{organizationId}`) SHALL still be updated immediately so that feature gates are enforced without delay.

### Requirement 13: API Response Envelope and Error Handling

**User Story:** As a consumer of the BillingService API, I want consistent response formats and meaningful error codes so that integration is predictable and debugging is straightforward.

#### Acceptance Criteria

1. THE BillingService SHALL wrap all API responses in the `ApiResponse<T>` envelope with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`, and `Errors` fields.
2. WHEN a `DomainException` is thrown, THE GlobalExceptionHandlerMiddleware SHALL catch the exception and return the appropriate HTTP status code with the error code and message in the `ApiResponse` envelope.
3. WHEN a FluentValidation failure occurs, THE BillingService SHALL return HTTP 422 with `VALIDATION_ERROR` (1000) and include field-level error details in the `Errors` array.
4. THE BillingService SHALL define the following error codes in the 5000 range:

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| SUBSCRIPTION_ALREADY_EXISTS | 5001 | 409 | Organization already has an active subscription |
| PLAN_NOT_FOUND | 5002 | 404 | Specified plan does not exist or is inactive |
| SUBSCRIPTION_NOT_FOUND | 5003 | 404 | No subscription found for the organization |
| INVALID_UPGRADE_PATH | 5004 | 400 | Target plan is not a higher tier |
| NO_ACTIVE_SUBSCRIPTION | 5005 | 400 | Organization has no active or trialing subscription |
| INVALID_DOWNGRADE_PATH | 5006 | 400 | Target plan is not a lower tier |
| USAGE_EXCEEDS_PLAN_LIMITS | 5007 | 400 | Current usage exceeds target plan limits |
| SUBSCRIPTION_ALREADY_CANCELLED | 5008 | 400 | Subscription is already cancelled |
| TRIAL_EXPIRED | 5009 | 400 | Trial period has ended |
| PAYMENT_PROVIDER_ERROR | 5010 | 502 | Stripe API error |
| INVALID_WEBHOOK_SIGNATURE | 5011 | 400 | Stripe webhook signature verification failed |
| INVALID_WEBHOOK_PAYLOAD | 5012 | 400 | Webhook payload could not be deserialized |
| FEATURE_NOT_AVAILABLE | 5013 | 403 | Feature not included in current plan |
| USAGE_LIMIT_REACHED | 5014 | 403 | Organization has reached the usage limit for this feature |

5. WHEN an unhandled exception occurs, THE GlobalExceptionHandlerMiddleware SHALL return HTTP 500 with a generic error message and log the full exception details without exposing internals to the client.

### Requirement 14: Middleware Pipeline and Security

**User Story:** As the platform, I want BillingService to enforce the same security and observability patterns as other services so that authentication, authorization, rate limiting, and tracing are consistent.

#### Acceptance Criteria

1. THE BillingService SHALL configure the middleware pipeline in the following order: CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope → Controllers.
2. WHEN a request includes an `X-Correlation-Id` header, THE CorrelationIdMiddleware SHALL propagate the value. WHEN the header is absent, THE CorrelationIdMiddleware SHALL generate a new GUID and attach it to the response and `HttpContext.Items`.
3. WHEN an authenticated request is received, THE JwtClaimsMiddleware SHALL extract `userId`, `organizationId`, `departmentId`, `roleName`, and `deviceId` from JWT claims and store them in `HttpContext.Items`.
4. WHEN a request targets an endpoint requiring `OrgAdmin` role, THE RoleAuthorizationMiddleware SHALL validate the user's role. IF the role is insufficient, THEN THE BillingService SHALL return HTTP 403 with `INSUFFICIENT_PERMISSIONS`.
5. WHEN a request targets an organization-scoped endpoint, THE OrganizationScopeMiddleware SHALL validate that the `organizationId` from JWT claims matches any `organizationId` in route or query parameters. IF there is a mismatch, THEN THE BillingService SHALL return HTTP 403 with `ORGANIZATION_MISMATCH`.
6. THE BillingService SHALL apply rate limiting using the sliding window algorithm via Redis. The Stripe webhook endpoint (`POST /api/v1/webhooks/stripe`) SHALL be exempt from user-based rate limiting but SHALL enforce IP-based rate limiting.
7. WHEN the Stripe webhook endpoint is called, THE BillingService SHALL skip JWT authentication and instead validate the request using the Stripe webhook signature.

### Requirement 15: Data Model and Persistence

**User Story:** As a developer, I want a well-defined data model so that billing data is stored consistently and supports all subscription lifecycle operations.

#### Acceptance Criteria

1. THE BillingService SHALL define a `BillingDbContext` with EF Core using Npgsql for PostgreSQL, targeting database `nexus_billing`.
2. THE BillingService SHALL define the following entities:

**Subscription entity:**
- `SubscriptionId` (Guid, PK)
- `OrganizationId` (Guid, unique index, required)
- `PlanId` (Guid, FK to Plan, required)
- `Status` (string, required): `Active`, `Trialing`, `PastDue`, `Cancelled`, `Expired`
- `ExternalSubscriptionId` (string, nullable): Stripe subscription ID
- `ExternalCustomerId` (string, nullable): Stripe customer ID
- `CurrentPeriodStart` (DateTime, required)
- `CurrentPeriodEnd` (DateTime, nullable)
- `TrialEndDate` (DateTime, nullable)
- `CancelledAt` (DateTime, nullable)
- `ScheduledPlanId` (Guid, nullable, FK to Plan): Plan to switch to at period end (for downgrades)
- `DateCreated` (DateTime, required)
- `DateUpdated` (DateTime, required)

**Plan entity:**
- `PlanId` (Guid, PK)
- `PlanName` (string, required, max 50)
- `PlanCode` (string, required, unique index, max 20)
- `TierLevel` (int, required): 0=Free, 1=Starter, 2=Professional, 3=Enterprise (for ordering comparisons)
- `MaxTeamMembers` (int, required): 0 = unlimited
- `MaxDepartments` (int, required): 0 = unlimited
- `MaxStoriesPerMonth` (int, required): 0 = unlimited
- `FeaturesJson` (string, JSON column, nullable)
- `PriceMonthly` (decimal, required)
- `PriceYearly` (decimal, required)
- `IsActive` (bool, required, default true)
- `DateCreated` (DateTime, required)

**UsageRecord entity:**
- `UsageRecordId` (Guid, PK)
- `OrganizationId` (Guid, indexed, required)
- `MetricName` (string, required, max 50): `active_members`, `stories_created`, `storage_bytes`
- `MetricValue` (long, required)
- `PeriodStart` (DateTime, required)
- `PeriodEnd` (DateTime, required)
- `DateUpdated` (DateTime, required)

**StripeEvent entity (for idempotent webhook processing):**
- `StripeEventId` (string, PK): Stripe event ID
- `EventType` (string, required, max 100)
- `ProcessedAt` (DateTime, required)

3. THE BillingService SHALL apply a unique index on `Subscription.OrganizationId` to enforce one subscription per organization.
4. THE BillingService SHALL apply a unique index on `Plan.PlanCode`.
5. THE BillingService SHALL apply a composite index on `(UsageRecord.OrganizationId, UsageRecord.MetricName, UsageRecord.PeriodStart)` for efficient usage queries.
6. THE BillingService SHALL configure `FeaturesJson` as a `jsonb` column type in PostgreSQL.

### Requirement 16: Redis Caching Patterns

**User Story:** As the platform, I want billing data cached in Redis so that feature gate checks and usage lookups are fast and do not overload the database.

#### Acceptance Criteria

1. THE BillingService SHALL cache plan tier and feature flags per organization in Redis with key `plan:{organizationId}` and 60-minute TTL. The cached value SHALL include `PlanCode`, `PlanName`, `TierLevel`, `MaxTeamMembers`, `MaxDepartments`, `MaxStoriesPerMonth`, and `FeaturesJson`.
2. THE BillingService SHALL cache usage counters per organization and metric in Redis with key `usage:{organizationId}:{metric}` and 5-minute TTL.
3. WHEN a plan change occurs, THE BillingService SHALL immediately invalidate and refresh the `plan:{organizationId}` cache entry.
4. WHEN a usage metric is incremented, THE BillingService SHALL update the Redis counter atomically using `INCRBY` and persist to the database periodically.
5. THE BillingService SHALL use the outbox queue `outbox:billing` for publishing audit events, following the same Redis outbox pattern as other services.
6. THE BillingService SHALL cache the service-to-service JWT in Redis with key `service_token:billing` and 23-hour TTL.

### Requirement 17: Health Checks and Observability

**User Story:** As a platform operator, I want health check endpoints so that load balancers and orchestrators can monitor BillingService availability.

#### Acceptance Criteria

1. WHEN `GET /health` is called, THE BillingService SHALL return HTTP 200 with a health status indicating the service is running.
2. WHEN `GET /ready` is called, THE BillingService SHALL check connectivity to PostgreSQL (`nexus_billing`) and Redis, and return HTTP 200 if all dependencies are reachable. IF any dependency is unreachable, THEN THE BillingService SHALL return HTTP 503 with details of the failing dependency.
3. THE BillingService SHALL register health checks for PostgreSQL and Redis using the ASP.NET Core health check framework.
4. THE BillingService SHALL include Swagger/OpenAPI documentation for all endpoints, accessible at `/swagger` in development mode.

### Requirement 18: Input Validation

**User Story:** As a developer, I want all API inputs validated so that invalid data is rejected early with clear error messages.

#### Acceptance Criteria

1. WHEN `POST /api/v1/subscriptions` is called, THE BillingService SHALL validate that `planId` is a non-empty GUID. IF validation fails, THEN THE BillingService SHALL return HTTP 422 with `VALIDATION_ERROR` (1000).
2. WHEN `PATCH /api/v1/subscriptions/upgrade` is called, THE BillingService SHALL validate that `newPlanId` is a non-empty GUID.
3. WHEN `PATCH /api/v1/subscriptions/downgrade` is called, THE BillingService SHALL validate that `newPlanId` is a non-empty GUID.
4. WHEN `POST /api/v1/usage/increment` is called, THE BillingService SHALL validate that `metricName` is one of the allowed values (`active_members`, `stories_created`, `storage_bytes`) and that `value` is a positive integer.
5. THE BillingService SHALL use FluentValidation for all request DTOs, registered in the DI container.
6. WHEN `POST /api/v1/webhooks/stripe` is called, THE BillingService SHALL validate the presence of the `Stripe-Signature` header before processing. IF the header is missing, THEN THE BillingService SHALL return HTTP 400 with `INVALID_WEBHOOK_SIGNATURE` (5011).
