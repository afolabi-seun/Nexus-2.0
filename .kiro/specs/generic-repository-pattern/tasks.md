# Implementation Plan: Generic Repository Pattern

## Overview

Incremental migration of ~43 repositories across 5 services to use `IGenericRepository<T>` / `GenericRepository<T>`. Services are migrated in order of complexity: SecurityService → BillingService → UtilityService → ProfileService → WorkService. Each service follows a 4-phase approach: add generic files, migrate simple repos, migrate complex repos, move `SaveChangesAsync` to the service layer.

## Tasks

- [x] 1. SecurityService — Add generic files and migrate repositories
  - [x] 1.1 Create `IGenericRepository<T>` interface and `GenericRepository<T>` implementation for SecurityService
    - Create `src/backend/SecurityService/SecurityService.Domain/Interfaces/Repositories/Generics/IGenericRepository.cs` with all 10 methods + `FindWithoutFiltersAsync`
    - Create `src/backend/SecurityService/SecurityService.Infrastructure/Repositories/Generics/GenericRepository.cs` accepting `DbContext` in constructor
    - Use namespace `SecurityService.Domain.Interfaces.Repositories.Generics` and `SecurityService.Infrastructure.Repositories.Generics`
    - _Requirements: 1.1–1.11, 2.1–2.13, 6.1–6.2, 7.1–7.3_

  - [ ]* 1.2 Write property tests for SecurityService GenericRepository
    - **Property 1: Add-then-retrieve round trip**
    - **Property 4: Single-entity mutation sets correct EntityState**
    - **Property 6: Mutation methods do not persist changes**
    - **Validates: Requirements 2.3, 2.6, 2.7, 2.8, 3.1**

  - [x] 1.3 Migrate PasswordHistoryRepository to extend GenericRepository
    - Update `IPasswordHistoryRepository` to extend `IGenericRepository<PasswordHistory>`, keep only `GetLastNByUserIdAsync`
    - Update `PasswordHistoryRepository` to extend `GenericRepository<PasswordHistory>`, pass `SecurityDbContext` to `base(db)`
    - Remove duplicated `AddAsync` implementation (now inherited)
    - _Requirements: 4.1–4.2, 5.1–5.4, 9.1–9.2_

  - [x] 1.4 Migrate ServiceTokenRepository to extend GenericRepository
    - Update `IServiceTokenRepository` to extend `IGenericRepository<ServiceToken>`
    - Update `ServiceTokenRepository` to extend `GenericRepository<ServiceToken>`, pass `SecurityDbContext` to `base(db)`
    - Remove all duplicated CRUD methods now inherited from GenericRepository
    - _Requirements: 4.1–4.3, 5.1–5.4_

  - [x] 1.5 Move SaveChangesAsync to service layer for SecurityService
    - Update service methods that call PasswordHistoryRepository and ServiceTokenRepository mutation methods to call `SaveChangesAsync` on `SecurityDbContext` after repository operations
    - Inject `SecurityDbContext` into service classes that don't already have it
    - _Requirements: 3.1–3.3, 10.6_

  - [ ]* 1.6 Write unit tests for SecurityService migration
    - Verify DI resolution of `IPasswordHistoryRepository` and `IServiceTokenRepository`
    - Verify existing tests pass without assertion changes
    - _Requirements: 8.1, 9.3_

- [x] 2. Checkpoint — SecurityService build verification
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. BillingService — Add generic files and migrate repositories
  - [x] 3.1 Create `IGenericRepository<T>` interface and `GenericRepository<T>` implementation for BillingService
    - Create `src/backend/BillingService/BillingService.Domain/Interfaces/Repositories/Generics/IGenericRepository.cs`
    - Create `src/backend/BillingService/BillingService.Infrastructure/Repositories/Generics/GenericRepository.cs`
    - Same interface/implementation as SecurityService, different namespace (`BillingService.Domain.Interfaces.Repositories.Generics`)
    - _Requirements: 1.1–1.11, 2.1–2.13, 6.1–6.2, 7.1–7.3_

  - [x] 3.2 Migrate UsageRecordRepository and StripeEventRepository (simple repos)
    - Update `IUsageRecordRepository` to extend `IGenericRepository<UsageRecord>`, remove CRUD declarations
    - Update `UsageRecordRepository` to extend `GenericRepository<UsageRecord>`, pass `BillingDbContext` to `base(db)`
    - Update `IStripeEventRepository` to extend `IGenericRepository<StripeEvent>`, remove CRUD declarations
    - Update `StripeEventRepository` to extend `GenericRepository<StripeEvent>`, pass `BillingDbContext` to `base(db)`
    - _Requirements: 4.1–4.3, 5.1–5.4_

  - [x] 3.3 Migrate PlanRepository (custom: GetByCodeAsync, GetAllActiveAsync, ExistsByCodeAsync)
    - Update `IPlanRepository` to extend `IGenericRepository<Plan>`, keep only `GetByCodeAsync`, `GetAllActiveAsync`, `ExistsByCodeAsync`
    - Remove `GetByIdAsync`, `GetAllAsync`, `CreateAsync`, `UpdateAsync` from interface (now inherited)
    - Update `PlanRepository` to extend `GenericRepository<Plan>`, pass `BillingDbContext` to `base(db)`
    - Remove duplicated CRUD implementations, keep custom query methods
    - Note: `GetAllAsync` in PlanRepository has custom ordering (`OrderBy(p => p.TierLevel)`) — override `GetAllAsync` or keep as a separate named method if ordering is required
    - _Requirements: 4.1–4.2, 5.1–5.4, 10.5_

  - [x] 3.4 Migrate SubscriptionRepository (custom: uses IgnoreQueryFilters)
    - Update `ISubscriptionRepository` to extend `IGenericRepository<Subscription>`, keep `GetByOrganizationIdAsync`, `GetExpiredTrialsAsync`, `GetSubscriptionsDueForDowngradeAsync`, `GetAllWithPlansAsync`, `GetCountByStatusAsync`
    - Remove `GetByIdAsync`, `CreateAsync`, `UpdateAsync` from interface
    - Update `SubscriptionRepository` to extend `GenericRepository<Subscription>`, pass `BillingDbContext` to `base(db)`
    - Ensure cross-org queries use `FindWithoutFiltersAsync` or direct `_db.Subscriptions.IgnoreQueryFilters()` as needed
    - _Requirements: 4.1–4.2, 5.1–5.4, 6.1–6.3, 10.4_

  - [x] 3.5 Move SaveChangesAsync to service layer for BillingService
    - Update `PlanService`, `SubscriptionService`, `UsageService`, `StripeWebhookService`, `AdminPlanService`, `AdminBillingService` to call `SaveChangesAsync` on `BillingDbContext` after repository mutation operations
    - Inject `BillingDbContext` into service classes that don't already have it
    - Remove `SaveChangesAsync` calls from within repository methods (e.g., `PlanRepository.CreateAsync`, `PlanRepository.UpdateAsync`)
    - _Requirements: 3.1–3.3, 10.6_

  - [ ]* 3.6 Write property tests for BillingService GenericRepository
    - **Property 2: GetAllAsync returns all tracked entities**
    - **Property 3: FindAsync filters correctly**
    - **Property 5: Range mutation sets correct EntityState for all entities**
    - **Validates: Requirements 2.4, 2.5, 2.9–2.11**

  - [ ]* 3.7 Write unit tests for BillingService migration
    - Verify DI resolution of all 4 billing repositories
    - Verify existing tests pass without assertion changes
    - _Requirements: 8.1, 9.3_

- [x] 4. Checkpoint — BillingService build verification
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. UtilityService — Add generic files and migrate repositories
  - [x] 5.1 Create `IGenericRepository<T>` interface and `GenericRepository<T>` implementation for UtilityService
    - Create `src/backend/UtilityService/UtilityService.Domain/Interfaces/Repositories/Generics/IGenericRepository.cs`
    - Create `src/backend/UtilityService/UtilityService.Infrastructure/Repositories/Generics/GenericRepository.cs`
    - Namespace: `UtilityService.Domain.Interfaces.Repositories.Generics` / `UtilityService.Infrastructure.Repositories.Generics`
    - _Requirements: 1.1–1.11, 2.1–2.13, 6.1–6.2, 7.1–7.3_

  - [x] 5.2 Migrate simple UtilityService repositories (6 repos)
    - Migrate `ArchivedAuditLogRepository` — extend GenericRepository, remove CRUD
    - Migrate `ErrorCodeEntryRepository` — extend GenericRepository, remove CRUD
    - Migrate `NotificationLogRepository` — extend GenericRepository, remove CRUD
    - Migrate `DepartmentTypeRepository` — extend GenericRepository, remove CRUD
    - Migrate `PriorityLevelRepository` — extend GenericRepository, remove CRUD
    - Migrate `TaskTypeRefRepository` — extend GenericRepository, remove CRUD
    - Migrate `WorkflowStateRepository` — extend GenericRepository, remove CRUD
    - For each: update interface to extend `IGenericRepository<T>`, update implementation to extend `GenericRepository<T>`, pass `UtilityDbContext` to `base(db)`
    - _Requirements: 4.1–4.3, 5.1–5.4_

  - [x] 5.3 Migrate complex UtilityService repositories (2 repos)
    - Migrate `AuditLogRepository` — keep custom `QueryAsync` with complex filtering (org, service, action, entity type, user, date range, pagination)
    - Migrate `ErrorLogRepository` — keep custom `QueryAsync` with complex filtering (org, service, error code, severity, date range, pagination)
    - Update interfaces to extend `IGenericRepository<T>`, remove `AddAsync` (now inherited), keep `QueryAsync`
    - Update implementations to extend `GenericRepository<T>`, pass `UtilityDbContext` to `base(db)`
    - _Requirements: 4.1–4.2, 5.1–5.4, 10.1–10.2, 10.5_

  - [x] 5.4 Move SaveChangesAsync to service layer for UtilityService
    - Update `AuditLogService`, `ErrorLogService`, `NotificationService`, `ReferenceDataService` and other service classes to call `SaveChangesAsync` on `UtilityDbContext` after repository mutation operations
    - Inject `UtilityDbContext` into service classes that don't already have it
    - _Requirements: 3.1–3.3, 10.6_

  - [ ]* 5.5 Write unit tests for UtilityService migration
    - Verify DI resolution of all 9 utility repositories
    - Verify existing tests pass without assertion changes
    - _Requirements: 8.1, 9.3_

- [x] 6. Checkpoint — UtilityService build verification
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. ProfileService — Add generic files and migrate repositories
  - [x] 7.1 Create `IGenericRepository<T>` interface and `GenericRepository<T>` implementation for ProfileService
    - Create `src/backend/ProfileService/ProfileService.Domain/Interfaces/Repositories/Generics/IGenericRepository.cs`
    - Create `src/backend/ProfileService/ProfileService.Infrastructure/Repositories/Generics/GenericRepository.cs`
    - Namespace: `ProfileService.Domain.Interfaces.Repositories.Generics` / `ProfileService.Infrastructure.Repositories.Generics`
    - _Requirements: 1.1–1.11, 2.1–2.13, 6.1–6.2, 7.1–7.3_

  - [x] 7.2 Migrate simple ProfileService repositories (6 repos)
    - Migrate `DepartmentMemberRepository` — extend GenericRepository, remove CRUD
    - Migrate `RoleRepository` — extend GenericRepository, remove CRUD
    - Migrate `DeviceRepository` — extend GenericRepository, remove CRUD
    - Migrate `NotificationSettingRepository` — extend GenericRepository, remove CRUD
    - Migrate `NotificationTypeRepository` — extend GenericRepository, remove CRUD
    - Migrate `UserPreferencesRepository` — extend GenericRepository, remove CRUD
    - Migrate `PlatformAdminRepository` — extend GenericRepository, remove CRUD
    - For each: update interface to extend `IGenericRepository<T>`, update implementation to extend `GenericRepository<T>`, pass `ProfileDbContext` to `base(db)`
    - _Requirements: 4.1–4.3, 5.1–5.4_

  - [x] 7.3 Migrate complex ProfileService repositories (4 repos)
    - Migrate `OrganizationRepository` — keep `GetByNameAsync`, `GetByStoryIdPrefixAsync`, `ListAllAsync`
    - Migrate `DepartmentRepository` — keep `ListAsync` with pagination
    - Migrate `TeamMemberRepository` — keep `GetByEmailAsync`, `GetByEmailGlobalAsync`, `ListAsync`, `CountOrgAdminsAsync`, `GetNextSequentialNumberAsync`; use `FindWithoutFiltersAsync` for `GetByEmailGlobalAsync` (cross-org query using IgnoreQueryFilters)
    - Migrate `InviteRepository` — keep `GetByTokenAsync`, `ListAsync`
    - For each: update interface to extend `IGenericRepository<T>`, remove CRUD declarations, update implementation to extend `GenericRepository<T>`, pass `ProfileDbContext` to `base(db)`
    - _Requirements: 4.1–4.2, 5.1–5.4, 6.1–6.3, 10.1–10.5_

  - [x] 7.4 Move SaveChangesAsync to service layer for ProfileService
    - Update `OrganizationService`, `DepartmentService`, `TeamMemberService`, `RoleService`, `InviteService`, `DeviceService`, `NotificationSettingService`, `PreferenceService`, `PlatformAdminService` to call `SaveChangesAsync` on `ProfileDbContext` after repository mutation operations
    - Inject `ProfileDbContext` into service classes that don't already have it
    - _Requirements: 3.1–3.3, 10.6_

  - [ ]* 7.5 Write property tests for ProfileService GenericRepository
    - **Property 7: FindWithoutFiltersAsync returns superset of FindAsync**
    - **Validates: Requirements 6.1, 6.2**

  - [ ]* 7.6 Write unit tests for ProfileService migration
    - Verify DI resolution of all 11 profile repositories
    - Verify existing tests pass without assertion changes
    - _Requirements: 8.1, 9.3_

- [x] 8. Checkpoint — ProfileService build verification
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. WorkService — Add generic files and migrate repositories
  - [x] 9.1 Create `IGenericRepository<T>` interface and `GenericRepository<T>` implementation for WorkService
    - Create `src/backend/WorkService/WorkService.Domain/Interfaces/Repositories/Generics/IGenericRepository.cs`
    - Create `src/backend/WorkService/WorkService.Infrastructure/Repositories/Generics/GenericRepository.cs`
    - Namespace: `WorkService.Domain.Interfaces.Repositories.Generics` / `WorkService.Infrastructure.Repositories.Generics`
    - _Requirements: 1.1–1.11, 2.1–2.13, 6.1–6.2, 7.1–7.3_

  - [x] 9.2 Migrate simple WorkService repositories (12 repos)
    - Migrate `TaskRepository` — extend GenericRepository, remove CRUD
    - Migrate `SprintRepository` — extend GenericRepository, remove CRUD
    - Migrate `SprintStoryRepository` — extend GenericRepository, remove CRUD
    - Migrate `CommentRepository` — extend GenericRepository, remove CRUD
    - Migrate `ActivityLogRepository` — extend GenericRepository, remove CRUD
    - Migrate `LabelRepository` — extend GenericRepository, remove CRUD
    - Migrate `StoryLabelRepository` — extend GenericRepository, remove CRUD
    - Migrate `StoryLinkRepository` — extend GenericRepository, remove CRUD
    - Migrate `StorySequenceRepository` — extend GenericRepository, remove CRUD
    - Migrate `SavedFilterRepository` — extend GenericRepository, remove CRUD
    - Migrate `TimeEntryRepository` — extend GenericRepository, remove CRUD
    - Migrate `CostRateRepository` — extend GenericRepository, remove CRUD
    - Migrate `TimePolicyRepository` — extend GenericRepository, remove CRUD
    - Migrate `TimeApprovalRepository` — extend GenericRepository, remove CRUD
    - For each: update interface to extend `IGenericRepository<T>`, update implementation to extend `GenericRepository<T>`, pass `WorkDbContext` to `base(db)`
    - _Requirements: 4.1–4.3, 5.1–5.4_

  - [x] 9.3 Migrate analytics/snapshot WorkService repositories (5 repos)
    - Migrate `CostSnapshotRepository` — extend GenericRepository, remove CRUD
    - Migrate `VelocitySnapshotRepository` — extend GenericRepository, remove CRUD
    - Migrate `ProjectHealthSnapshotRepository` — extend GenericRepository, remove CRUD
    - Migrate `ResourceAllocationSnapshotRepository` — extend GenericRepository, remove CRUD
    - Migrate `RiskRegisterRepository` — extend GenericRepository, remove CRUD
    - For each: update interface to extend `IGenericRepository<T>`, update implementation to extend `GenericRepository<T>`, pass `WorkDbContext` to `base(db)`
    - _Requirements: 4.1–4.3, 5.1–5.4_

  - [x] 9.4 Migrate complex WorkService repositories (2 repos)
    - Migrate `ProjectRepository` — keep `GetByKeyAsync`, `GetByNameAsync`, `ListAsync` (pagination + status filter), `GetStoryCountAsync`, `GetSprintCountAsync`
    - Migrate `StoryRepository` — keep `GetByKeyAsync`, `ListAsync` (complex filtering: project, status, priority, department, assignee, sprint, labels, date range), `SearchAsync` (full-text search with `tsvector`), `CountTasksAsync`, `CountCompletedTasksAsync`, `AllDevTasksDoneAsync`, `AllTasksDoneAsync`, `ExistsByProjectAsync`
    - For each: update interface to extend `IGenericRepository<T>`, remove `GetByIdAsync`/`AddAsync`/`UpdateAsync` declarations, update implementation to extend `GenericRepository<T>`, pass `WorkDbContext` to `base(db)`
    - Preserve all existing Include chains, pagination, full-text search, and cross-entity count logic
    - _Requirements: 4.1–4.2, 5.1–5.4, 10.1–10.5_

  - [x] 9.5 Move SaveChangesAsync to service layer for WorkService
    - Update `ProjectService`, `StoryService`, `TaskService`, `SprintService`, `CommentService`, `LabelService`, `ActivityLogService`, `TimeEntryService`, `CostRateService`, `TimePolicyService`, `TimerSessionService`, `RiskRegisterService`, `CostSnapshotHostedService`, `AnalyticsSnapshotHostedService` to call `SaveChangesAsync` on `WorkDbContext` after repository mutation operations
    - Inject `WorkDbContext` into service classes that don't already have it
    - This is the largest and riskiest phase — WorkService has the most service methods calling repository mutations
    - _Requirements: 3.1–3.3, 10.6_

  - [ ]* 9.6 Write property tests for WorkService GenericRepository
    - **Property 8: Service-specific interface has no duplicate generic method signatures**
    - **Property 9: Migrated repository behavioral equivalence**
    - **Validates: Requirements 4.2, 5.3, 9.2**

  - [ ]* 9.7 Write unit tests for WorkService migration
    - Verify DI resolution of all 21 work repositories
    - Verify existing tests pass without assertion changes
    - _Requirements: 8.1, 9.3_

- [x] 10. Checkpoint — WorkService build verification
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Final verification and DI registration cleanup
  - [x] 11.1 Verify DI registrations across all 5 services
    - Confirm each service's `DependencyInjection.cs` registers migrated repositories against their service-specific interfaces (not `IGenericRepository<T>` open generic)
    - Confirm no open-generic `IGenericRepository<>` registrations exist
    - _Requirements: 8.1–8.3_

  - [ ]* 11.2 Write cross-service property test for interface signature correctness
    - **Property 8: Service-specific interface has no duplicate generic method signatures** — run across all 5 services via reflection
    - **Validates: Requirements 4.2**

- [x] 12. Final checkpoint — Full solution build and test
  - Ensure all tests pass across all 5 services, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each service migration
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Phase 4 (SaveChangesAsync migration) is the riskiest step per service — service methods must inject DbContext and call SaveChangesAsync after repo operations
- The GenericRepository accepts `DbContext` (not typed), so the same base class pattern works across all services
- Existing tests should pass without assertion changes after migration
