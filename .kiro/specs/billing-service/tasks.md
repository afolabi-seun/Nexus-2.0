# Implementation Plan: BillingService

## Overview

Implement the BillingService microservice following Clean Architecture (.NET 8) with the same patterns as the existing four services. The service runs on port 5300 with database `nexus_billing` and handles subscription management, plan tiers, feature gating, usage tracking, trial management, and Stripe payment integration. Tasks are ordered to build incrementally: scaffolding → domain → application → infrastructure → API → testing → verification.

## Tasks

- [x] 1. Project scaffolding and solution setup
  - [x] 1.1 Create the five class library and web projects under `src/backend/BillingService/`
    - Create `BillingService.Domain` (class library, net8.0)
    - Create `BillingService.Application` (class library, net8.0, references Domain)
    - Create `BillingService.Infrastructure` (class library, net8.0, references Domain + Application)
    - Create `BillingService.Api` (web project, net8.0, references Application + Infrastructure)
    - Create `BillingService.Tests` (xunit test project, net8.0, references all four projects)
    - _Requirements: 15.1_

  - [x] 1.2 Add all five projects to `Nexus-2.0.sln` under a `BillingService` solution folder
    - Follow the same solution folder nesting pattern as SecurityService, ProfileService, WorkService, UtilityService
    - _Requirements: 15.1_

  - [x] 1.3 Configure NuGet packages for each project
    - **Domain**: No external packages (pure domain)
    - **Application**: `FluentValidation` (12.1.1)
    - **Infrastructure**: `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0.11), `StackExchange.Redis` (2.12.8), `Stripe.net` (47.4.0), `Microsoft.Extensions.Http.Polly` (10.0.5), `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0.11), `DotNetEnv` (3.1.1), `System.IdentityModel.Tokens.Jwt` (8.17.0)
    - **Api**: `DotNetEnv` (3.1.1), `FluentValidation.AspNetCore` (11.3.1), `Swashbuckle.AspNetCore` (6.6.2)
    - **Tests**: `xunit` (2.5.3), `xunit.runner.visualstudio` (2.5.3), `FsCheck.Xunit` (3.3.2), `Moq` (4.20.72), `Microsoft.EntityFrameworkCore.InMemory` (8.0.11), `Microsoft.NET.Test.Sdk` (17.8.0), `coverlet.collector` (6.0.0), `FluentValidation` (12.1.1), `Microsoft.AspNetCore.Mvc.Testing` (8.0.11)
    - _Requirements: 11.1, 11.7, 15.1_

- [x] 2. Domain layer — entities, enums, exceptions, and interfaces
  - [x] 2.1 Create domain entities
    - `Entities/Subscription.cs` — SubscriptionId, OrganizationId, PlanId, Status, ExternalSubscriptionId, ExternalCustomerId, CurrentPeriodStart, CurrentPeriodEnd, TrialEndDate, CancelledAt, ScheduledPlanId, DateCreated, DateUpdated
    - `Entities/Plan.cs` — PlanId, PlanName, PlanCode, TierLevel, MaxTeamMembers, MaxDepartments, MaxStoriesPerMonth, FeaturesJson, PriceMonthly, PriceYearly, IsActive, DateCreated
    - `Entities/UsageRecord.cs` — UsageRecordId, OrganizationId, MetricName, MetricValue, PeriodStart, PeriodEnd, DateUpdated
    - `Entities/StripeEvent.cs` — StripeEventId (string PK), EventType, ProcessedAt
    - `Common/IOrganizationEntity.cs` — marker interface with OrganizationId property
    - Subscription and UsageRecord implement IOrganizationEntity
    - _Requirements: 15.2_

  - [x] 2.2 Create domain enums
    - `Enums/SubscriptionStatus.cs` — Active, Trialing, PastDue, Cancelled, Expired
    - `Enums/MetricName.cs` — ActiveMembers, StoriesCreated, StorageBytes (with string constants `active_members`, `stories_created`, `storage_bytes`)
    - _Requirements: 15.2_

  - [x] 2.3 Create DomainException base class and ErrorCodes
    - `Exceptions/DomainException.cs` — base exception with ErrorValue, ErrorCode, StatusCode, CorrelationId properties
    - `Exceptions/ErrorCodes.cs` — static class with all 14 error codes (5001–5014) and InternalError constants
    - _Requirements: 13.4_

  - [x] 2.4 Create specific exception subclasses
    - `SubscriptionAlreadyExistsException` (5001, 409), `PlanNotFoundException` (5002, 404), `SubscriptionNotFoundException` (5003, 404), `InvalidUpgradePathException` (5004, 400), `NoActiveSubscriptionException` (5005, 400), `InvalidDowngradePathException` (5006, 400), `UsageExceedsPlanLimitsException` (5007, 400), `SubscriptionAlreadyCancelledException` (5008, 400), `TrialExpiredException` (5009, 400), `PaymentProviderException` (5010, 502), `InvalidWebhookSignatureException` (5011, 400), `InvalidWebhookPayloadException` (5012, 400), `FeatureNotAvailableException` (5013, 403), `UsageLimitReachedException` (5014, 403)
    - _Requirements: 13.4_

  - [x] 2.5 Create repository interfaces
    - `Interfaces/Repositories/ISubscriptionRepository.cs` — GetByOrganizationIdAsync, GetByIdAsync, CreateAsync, UpdateAsync, GetExpiredTrialsAsync, GetSubscriptionsDueForDowngradeAsync
    - `Interfaces/Repositories/IPlanRepository.cs` — GetByIdAsync, GetByCodeAsync, GetAllActiveAsync, CreateAsync, ExistsByCodeAsync
    - `Interfaces/Repositories/IUsageRecordRepository.cs` — GetByOrganizationAndPeriodAsync, UpsertAsync, ArchivePeriodAsync
    - `Interfaces/Repositories/IStripeEventRepository.cs` — ExistsAsync, CreateAsync
    - _Requirements: 15.2_

  - [x] 2.6 Create service interfaces
    - `Interfaces/Services/ISubscriptionService.cs` — GetCurrentAsync, CreateAsync, UpgradeAsync, DowngradeAsync, CancelAsync
    - `Interfaces/Services/IPlanService.cs` — GetAllActiveAsync, SeedPlansAsync
    - `Interfaces/Services/IFeatureGateService.cs` — CheckFeatureAsync
    - `Interfaces/Services/IUsageService.cs` — GetUsageAsync, IncrementAsync
    - `Interfaces/Services/IStripePaymentService.cs` — CreateSubscriptionAsync, UpdateSubscriptionAsync, CancelSubscriptionAtPeriodEndAsync, VerifyWebhookSignature
    - `Interfaces/Services/IOutboxService.cs` — PublishAsync
    - `Interfaces/Services/IErrorCodeResolverService.cs` — ResolveAsync
    - _Requirements: 2.1, 3.1, 4.1, 5.1, 6.1, 8.1, 9.1, 10.1, 11.1, 12.4_

- [x] 3. Checkpoint — Verify domain layer compiles
  - Ensure `dotnet build` succeeds for BillingService.Domain. Ask the user if questions arise.

- [x] 4. Application layer — DTOs, contracts, and validators
  - [x] 4.1 Create request and response DTOs
    - `DTOs/ApiResponse.cs` — generic ApiResponse<T> envelope with ResponseCode, Success, Data, ErrorCode, ErrorValue, Message, CorrelationId, ResponseCode, ResponseDescription
    - `DTOs/ErrorDetail.cs` — field-level error detail
    - `DTOs/OutboxMessage.cs` — outbox message DTO
    - `DTOs/Subscriptions/CreateSubscriptionRequest.cs`, `UpgradeSubscriptionRequest.cs`, `DowngradeSubscriptionRequest.cs`, `SubscriptionResponse.cs`, `SubscriptionDetailResponse.cs`
    - `DTOs/Plans/PlanResponse.cs`
    - `DTOs/Usage/IncrementUsageRequest.cs`, `UsageResponse.cs`, `UsageMetric.cs`
    - `DTOs/FeatureGates/FeatureGateResponse.cs`
    - `Contracts/OrganizationSettingsUpdateRequest.cs`
    - _Requirements: 13.1, 2.1, 3.1, 4.1, 5.1, 8.1, 9.1_

  - [x] 4.2 Create FluentValidation validators
    - `Validators/CreateSubscriptionRequestValidator.cs` — PlanId must be non-empty GUID
    - `Validators/UpgradeSubscriptionRequestValidator.cs` — NewPlanId must be non-empty GUID
    - `Validators/DowngradeSubscriptionRequestValidator.cs` — NewPlanId must be non-empty GUID
    - `Validators/IncrementUsageRequestValidator.cs` — MetricName must be one of allowed values, Value must be positive
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5_

- [x] 5. Infrastructure layer — data access, services, and integrations
  - [x] 5.1 Create BillingDbContext with EF Core configuration
    - Configure all four entities with proper column types, indexes, and constraints
    - Organization-scoped query filters on Subscription and UsageRecord (extract OrganizationId from HttpContext.Items)
    - Unique index on Subscription.OrganizationId, unique index on Plan.PlanCode
    - Composite index on (UsageRecord.OrganizationId, UsageRecord.MetricName, UsageRecord.PeriodStart)
    - FeaturesJson as jsonb column type
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6_

  - [x] 5.2 Create repository implementations
    - `Repositories/SubscriptionRepository.cs` — implements ISubscriptionRepository using BillingDbContext
    - `Repositories/PlanRepository.cs` — implements IPlanRepository
    - `Repositories/UsageRecordRepository.cs` — implements IUsageRecordRepository
    - `Repositories/StripeEventRepository.cs` — implements IStripeEventRepository
    - _Requirements: 15.2_

  - [x] 5.3 Implement PlanService with seed data
    - `Services/Plans/PlanService.cs` — GetAllActiveAsync (with Redis caching), SeedPlansAsync (idempotent seeding of 4 plan tiers)
    - Seed data: Free (tier 0), Starter (tier 1), Professional (tier 2), Enterprise (tier 3) with exact limits and pricing from requirements
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

  - [x] 5.4 Implement SubscriptionService
    - `Services/Subscriptions/SubscriptionService.cs` — full subscription lifecycle: create (with trial for paid plans, direct active for Free), upgrade (tier validation, proration, trial end), downgrade (scheduled at period end, usage validation), cancel (immediate for Free, period-end for paid), get current (with plan + usage details)
    - Redis cache refresh on every plan change, ProfileService notification, outbox audit events
    - _Requirements: 2.1–2.8, 3.1–3.3, 4.1–4.6, 5.1–5.6, 6.1–6.6_

  - [x] 5.5 Implement FeatureGateService
    - `Services/FeatureGates/FeatureGateService.cs` — CheckFeatureAsync: read plan from Redis cache (fallback to DB), compare current usage against plan limits, return allowed/denied with usage and limit values
    - _Requirements: 8.1–8.6_

  - [x] 5.6 Implement UsageService
    - `Services/Usage/UsageService.cs` — GetUsageAsync (read from Redis counters with DB fallback), IncrementAsync (Redis INCRBY), period reset logic
    - _Requirements: 9.1–9.6_

  - [x] 5.7 Implement StripePaymentService
    - `Services/Stripe/StripePaymentService.cs` — wraps Stripe .NET SDK: CreateSubscriptionAsync (create customer + subscription), UpdateSubscriptionAsync (proration), CancelSubscriptionAtPeriodEndAsync, VerifyWebhookSignature
    - Wrap Stripe errors in PaymentProviderException
    - _Requirements: 11.1–11.7_

  - [x] 5.8 Implement StripeWebhookService
    - `Services/Stripe/StripeWebhookService.cs` — process webhook events: invoice.payment_succeeded, invoice.payment_failed, customer.subscription.updated, customer.subscription.deleted
    - Idempotent processing via StripeEvent table
    - _Requirements: 10.1–10.8_

  - [x] 5.9 Implement OutboxService and ErrorCodeResolverService
    - `Services/Outbox/OutboxService.cs` — PublishAsync using Redis LPUSH to `outbox:billing`
    - `Services/ErrorCodeResolver/ErrorCodeResolverService.cs` — resolve error codes for GlobalExceptionHandler
    - _Requirements: 12.4, 16.5, 13.2_

  - [x] 5.10 Implement service clients
    - `Services/ServiceClients/IProfileServiceClient.cs` + `ProfileServiceClient.cs` — UpdateOrganizationPlanTierAsync via typed HttpClient with Polly
    - `Services/ServiceClients/ISecurityServiceClient.cs` + `SecurityServiceClient.cs` — GetServiceTokenAsync with Redis caching (23h TTL)
    - `Services/ServiceClients/CorrelationIdDelegatingHandler.cs` — propagate X-Correlation-Id on outgoing calls
    - _Requirements: 12.1, 12.2, 12.3, 12.5_

  - [x] 5.11 Implement background hosted services
    - `Services/BackgroundServices/TrialExpiryHostedService.cs` — runs every 1 hour, queries expired trials, transitions to Active or Expired/Free based on payment method
    - `Services/BackgroundServices/UsagePersistenceHostedService.cs` — runs every 5 minutes, persists Redis usage counters to UsageRecord table
    - Both use IServiceScopeFactory, structured logging, try/catch with Task.Delay loop
    - _Requirements: 7.2, 7.5, 9.2_

  - [x] 5.12 Create AppSettings, DatabaseMigrationHelper, and DependencyInjection
    - `Configuration/AppSettings.cs` — load from environment variables (DB connection, Redis, Stripe keys, JWT settings, service URLs, port 5300)
    - `Configuration/DatabaseMigrationHelper.cs` — apply EF Core migrations and seed plans at startup
    - `Configuration/DependencyInjection.cs` — register all repositories, services, service clients, Redis, DbContext, typed HttpClients with Polly policies, background services
    - _Requirements: 1.1, 11.7, 12.2, 15.1, 16.6_

- [x] 6. Checkpoint — Verify infrastructure layer compiles
  - Ensure `dotnet build` succeeds for BillingService.Infrastructure. Ask the user if questions arise.

- [x] 7. API layer — controllers, middleware, extensions, and Program.cs
  - [x] 7.1 Create middleware classes
    - `Middleware/CorrelationIdMiddleware.cs` — propagate or generate X-Correlation-Id
    - `Middleware/GlobalExceptionHandlerMiddleware.cs` — catch DomainException (warning log, ApiResponse) and unhandled Exception (error log, generic 500)
    - `Middleware/JwtClaimsMiddleware.cs` — extract userId, organizationId, departmentId, roleName, deviceId from JWT claims into HttpContext.Items
    - `Middleware/TokenBlacklistMiddleware.cs` — check Redis token blacklist
    - `Middleware/RateLimiterMiddleware.cs` — sliding window rate limiting via Redis, IP-based for webhook endpoint
    - `Middleware/RoleAuthorizationMiddleware.cs` — validate OrgAdmin role for protected endpoints
    - `Middleware/OrganizationScopeMiddleware.cs` — validate organizationId from JWT matches route/query params
    - _Requirements: 14.1–14.7_

  - [x] 7.2 Create controller attributes
    - `Attributes/OrgAdminAttribute.cs` — marks endpoints requiring OrgAdmin role
    - `Attributes/ServiceAuthAttribute.cs` — marks endpoints requiring service-to-service JWT
    - _Requirements: 14.4, 14.7_

  - [x] 7.3 Create API extension classes
    - `Extensions/MiddlewarePipelineExtensions.cs` — UseBillingPipeline() with correct middleware order: CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope
    - `Extensions/ControllerServiceExtensions.cs` — AddApiControllers()
    - `Extensions/SwaggerServiceExtensions.cs` — AddSwaggerServices(), UseSwaggerInDevelopment()
    - `Extensions/HealthCheckExtensions.cs` — AddHealthCheckServices() with PostgreSQL + Redis checks, MapHealthCheckEndpoints() for /health and /ready
    - _Requirements: 14.1, 17.1, 17.2, 17.3, 17.4_

  - [x] 7.4 Create API controllers
    - `Controllers/SubscriptionController.cs` — GET /current, POST, PATCH /upgrade, PATCH /downgrade, POST /cancel (OrgAdmin auth)
    - `Controllers/PlanController.cs` — GET (Bearer auth)
    - `Controllers/UsageController.cs` — GET (OrgAdmin), POST /increment (Service auth)
    - `Controllers/FeatureGateController.cs` — GET /{feature}?organizationId={id} (Service auth)
    - `Controllers/StripeWebhookController.cs` — POST with [AllowAnonymous], validates Stripe signature
    - All controllers use `api/v1/` route prefix and return ApiResponse<T> envelope
    - _Requirements: 2.1, 3.1, 4.1, 5.1, 6.1, 8.1, 9.1, 9.3, 10.1_

  - [x] 7.5 Create Program.cs and configuration files
    - `Program.cs` — load .env, build AppSettings, register infrastructure services, controllers, FluentValidation, JWT Bearer auth, CORS, health checks, Swagger, apply migrations, configure middleware pipeline, map controllers and health endpoints
    - `appsettings.json` and `appsettings.Development.json` — base configuration
    - `.env.example` — template for required environment variables
    - `Dockerfile` — multi-stage build for .NET 8
    - `Properties/launchSettings.json` — port 5300
    - _Requirements: 14.1, 15.1, 17.4_

- [x] 8. Checkpoint — Verify full solution compiles
  - Run `dotnet build` for the entire BillingService solution. Ensure all five projects compile without errors. Ask the user if questions arise.

- [x] 9. Property-based tests
  - [x] 9.1 Create FsCheck generators
    - `Tests/Property/Generators/PlanGenerator.cs` — generates plans with valid tier levels (0–3), non-negative limits, valid FeaturesJson
    - `Tests/Property/Generators/SubscriptionGenerator.cs` — generates subscriptions with valid statuses, consistent dates, valid FK references
    - `Tests/Property/Generators/UsageRecordGenerator.cs` — generates usage records with valid metric names, non-negative values
    - `Tests/Property/Generators/StripeEventGenerator.cs` — generates Stripe event payloads for the four handled event types

  - [x] 9.2 Write property tests for plan management
    - **Property 1: Active plan filtering** — GetAllActiveAsync returns exactly plans where IsActive == true
    - **Validates: Requirements 1.2**
    - **Property 2: FeaturesJson round-trip serialization** — serialize/deserialize produces equivalent object
    - **Validates: Requirements 1.5**
    - **Property 3: Plan seeding idempotency** — running seed twice produces same plans, no duplicates
    - **Validates: Requirements 1.6**
    - **Property 35: Plan cache consistency** — cached plan data matches database Plan record
    - **Validates: Requirements 1.4, 8.3, 16.1**
    - File: `Tests/Property/PlanPropertyTests.cs`

  - [x] 9.3 Write property tests for subscription lifecycle
    - **Property 4: Paid plan subscription creates with trial** — paid plan → Status=Trialing, TrialEndDate = creation + 14 days
    - **Validates: Requirements 2.1, 7.1**
    - **Property 5: One subscription per organization** — duplicate creation fails with SUBSCRIPTION_ALREADY_EXISTS
    - **Validates: Requirements 2.2, 15.3**
    - **Property 6: Free plan subscription skips Stripe** — Free plan → Status=Active, no Stripe call
    - **Validates: Requirements 2.4**
    - **Property 10: Tier ordering enforcement** — upgrade only if target tier > current, downgrade only if target tier < current
    - **Validates: Requirements 4.1, 5.1**
    - **Property 11: Upgrade during trial ends trial immediately** — Trialing + upgrade → Active, TrialEndDate cleared
    - **Validates: Requirements 4.6, 7.4**
    - **Property 12: Downgrade scheduled at period end** — ScheduledPlanId set, PlanId unchanged
    - **Validates: Requirements 5.2**
    - **Property 14: Cancellation records status and timestamp** — Status=Cancelled, CancelledAt set
    - **Validates: Requirements 6.1**
    - **Property 15: Free plan cancellation skips Stripe** — Free plan cancel → no Stripe call
    - **Validates: Requirements 6.6**
    - **Property 16: Trial expiry transitions based on payment method** — payment method → Active; no payment method → Expired + Free
    - **Validates: Requirements 7.2**
    - **Property 19: Plan change propagates to cache and ProfileService** — Redis refreshed, ProfileService called
    - **Validates: Requirements 2.5, 4.3, 5.3, 6.2, 8.4, 12.1, 16.3**
    - **Property 20: Billing state changes publish audit events** — correct event type and orgId published to outbox
    - **Validates: Requirements 2.6, 4.4, 5.5, 6.3, 7.6, 10.7, 12.4**
    - **Property 37: Stripe Customer ID stored on subscription** — paid plan creation stores ExternalCustomerId
    - **Validates: Requirements 11.6**
    - File: `Tests/Property/SubscriptionLifecyclePropertyTests.cs`

  - [x] 9.4 Write property tests for feature gates
    - **Property 17: Trialing subscriptions grant full plan access** — Trialing + plan feature → allowed=true
    - **Validates: Requirements 7.3**
    - **Property 18: Feature gate response correctness** — allowed=true when usage < limit, allowed=false when usage >= limit
    - **Validates: Requirements 8.1, 8.5, 8.6**
    - **Property 36: ProfileService unavailability still updates Redis** — Redis updated even when ProfileService unreachable
    - **Validates: Requirements 12.5**
    - File: `Tests/Property/FeatureGatePropertyTests.cs`

  - [x] 9.5 Write property tests for usage tracking
    - **Property 21: Usage increment atomicity** — INCRBY increases counter by exact value
    - **Validates: Requirements 9.3, 16.4**
    - **Property 22: Usage response includes value and limit per metric** — response includes currentValue and limit for each metric
    - **Validates: Requirements 9.1, 9.4**
    - **Property 23: Billing period reset clears period-scoped counters** — stories_created reset to 0, previous period archived
    - **Validates: Requirements 9.5**
    - **Property 34: Usage metric validation** — invalid metricName or non-positive value → VALIDATION_ERROR
    - **Validates: Requirements 18.4**
    - File: `Tests/Property/UsagePropertyTests.cs`

  - [x] 9.6 Write property tests for webhooks
    - **Property 24: Webhook signature verification** — invalid signature → INVALID_WEBHOOK_SIGNATURE, no state change
    - **Validates: Requirements 10.1, 18.6**
    - **Property 25: Webhook idempotency** — duplicate event → no-op
    - **Validates: Requirements 10.6**
    - **Property 26: Webhook event processing updates subscription state** — correct state transitions per event type
    - **Validates: Requirements 10.2, 10.3, 10.4, 10.5**
    - **Property 27: Stripe errors wrapped in DomainException** — Stripe error → PaymentProviderException with message
    - **Validates: Requirements 11.5**
    - File: `Tests/Property/WebhookPropertyTests.cs`

  - [x] 9.7 Write property tests for validation and error handling
    - **Property 28: DomainException returns correct HTTP status and envelope** — correct StatusCode, ErrorCode, ErrorValue, CorrelationId
    - **Validates: Requirements 13.2**
    - **Property 29: Validation failures return 422 with field errors** — FluentValidation failure → 422 + VALIDATION_ERROR + field details
    - **Validates: Requirements 13.3, 18.1, 18.2, 18.3**
    - **Property 30: Unhandled exceptions return 500 without internals** — generic message, no stack trace exposed
    - **Validates: Requirements 13.5**
    - **Property 31: Correlation ID propagation** — present header → same value; absent → new GUID generated
    - **Validates: Requirements 14.2**
    - **Property 32: OrgAdmin role enforcement** — non-OrgAdmin → 403 INSUFFICIENT_PERMISSIONS
    - **Validates: Requirements 14.4**
    - **Property 33: Organization scope validation** — JWT orgId mismatch → 403 ORGANIZATION_MISMATCH
    - **Validates: Requirements 14.5**
    - **Property 38: ApiResponse envelope wraps all responses** — all responses contain ResponseCode, Success, Data/ErrorCode, CorrelationId
    - **Validates: Requirements 13.1**
    - Files: `Tests/Property/ValidationPropertyTests.cs`, `Tests/Property/ErrorHandlingPropertyTests.cs`

- [x] 10. Unit tests
  - [x] 10.1 Write unit tests for services
    - `Tests/Unit/Services/SubscriptionServiceTests.cs` — test specific scenarios: double cancellation, no subscription defaults, Stripe failure rollback, upgrade from Free to Starter
    - `Tests/Unit/Services/PlanServiceTests.cs` — test seed data values match requirement table, inactive plan filtering
    - `Tests/Unit/Services/FeatureGateServiceTests.cs` — test unlimited (0) limits, boolean feature flags (custom_workflows, priority_support)
    - `Tests/Unit/Services/UsageServiceTests.cs` — test no-subscription defaults to Free limits, period boundary handling
    - `Tests/Unit/Services/StripeWebhookServiceTests.cs` — test each event type handler, invalid payload handling
    - _Requirements: 1.1, 1.3, 2.2, 2.8, 6.4, 8.2, 9.6, 10.8_

  - [x] 10.2 Write unit tests for validators
    - `Tests/Unit/Validators/CreateSubscriptionRequestValidatorTests.cs` — empty GUID, valid GUID
    - `Tests/Unit/Validators/UpgradeSubscriptionRequestValidatorTests.cs` — empty GUID, valid GUID
    - `Tests/Unit/Validators/DowngradeSubscriptionRequestValidatorTests.cs` — empty GUID, valid GUID
    - `Tests/Unit/Validators/IncrementUsageRequestValidatorTests.cs` — invalid metric name, zero/negative value, valid inputs
    - _Requirements: 18.1, 18.2, 18.3, 18.4_

  - [x] 10.3 Write unit tests for middleware and controllers
    - `Tests/Unit/Middleware/GlobalExceptionHandlerMiddlewareTests.cs` — DomainException handling, unhandled exception handling, correlation ID in response
    - `Tests/Unit/Controllers/SubscriptionControllerTests.cs` — verify correct status codes, response envelope structure
    - _Requirements: 13.2, 13.5, 14.2_

- [x] 11. Final checkpoint — Verify build and tests pass
  - Run `dotnet build` for the entire solution to confirm no compilation errors
  - Run `dotnet test` for BillingService.Tests to confirm all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each major layer
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples, edge cases, and error conditions
- The implementation follows the same Clean Architecture patterns as ProfileService, SecurityService, WorkService, and UtilityService
