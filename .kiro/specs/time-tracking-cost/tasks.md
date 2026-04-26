# Implementation Plan: Time Tracking & Cost Data

## Overview

Implements time tracking, cost rate management, approval workflows, timer sessions, and cost analytics in WorkService. Tasks are ordered by dependency: domain entities and exceptions first, then repository interfaces and implementations, then services, then controllers and DTOs, then background services, then tests. All new code follows existing WorkService patterns (IOrganizationEntity, FlgStatus, ApiResponse<T>, FluentValidation, outbox).

## Tasks

- [x] 1. Domain layer — entities, exceptions, and error codes
  - [x] 1.1 Create domain entities (TimeEntry, CostRate, TimePolicy, TimeApproval, CostSnapshot)
    - Add five new entity classes in `WorkService.Domain/Entities/`
    - Each entity implements `IOrganizationEntity` with `FlgStatus` soft-delete
    - Follow exact property definitions from the design document
    - _Requirements: 1.1, 2.2, 6.1, 8.1, 5.1, 12.1, 13.1_

  - [x] 1.2 Add time tracking error codes to ErrorCodes.cs
    - Add error codes 4050–4056: `TIMER_ALREADY_ACTIVE`, `NO_ACTIVE_TIMER`, `TIME_ENTRY_NOT_FOUND`, `COST_RATE_DUPLICATE`, `INVALID_COST_RATE`, `INVALID_TIME_POLICY`, `DAILY_HOURS_EXCEEDED`
    - _Requirements: 15.1_

  - [x] 1.3 Create domain exception classes
    - Add `TimerAlreadyActiveException`, `NoActiveTimerException`, `TimeEntryNotFoundException`, `CostRateDuplicateException`, `InvalidCostRateException`, `InvalidTimePolicyException`, `DailyHoursExceededException` in `WorkService.Domain/Exceptions/`
    - Each extends `DomainException` with correct error code, value, and HTTP status
    - _Requirements: 15.1, 15.2_

- [x] 2. Domain layer — repository and service interfaces
  - [x] 2.1 Create repository interfaces
    - Add `ITimeEntryRepository`, `ICostRateRepository`, `ITimePolicyRepository`, `ITimeApprovalRepository`, `ICostSnapshotRepository`
    - Place in `Domain/Interfaces/Repositories/{EntityName}/` subfolders matching infrastructure convention
    - Follow exact method signatures from the design document
    - _Requirements: 1.1, 4.1, 6.1, 6.4, 8.1, 12.1_

  - [x] 2.2 Create service interfaces
    - Add `ITimeEntryService`, `ICostRateService`, `ICostRateResolver`, `ITimePolicyService`, `ITimerSessionService`, `ICostSnapshotService`
    - Place in `Domain/Interfaces/Services/{ServiceName}/` subfolders
    - `ICostRateResolver.Resolve()` is a pure function — takes pre-fetched rate collections, returns decimal
    - _Requirements: 7.1, 7.4, 2.1, 9.1, 10.1, 11.1_

- [x] 3. Checkpoint — Ensure domain layer compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Application layer — DTOs and validators
  - [x] 4.1 Create time entry DTOs
    - Add `CreateTimeEntryRequest`, `UpdateTimeEntryRequest`, `TimeEntryResponse`, `TimerStartRequest`, `TimerStatusResponse`, `RejectTimeEntryRequest`, `ProjectCostSummaryResponse`, `MemberCostDetail`, `DepartmentCostDetail`, `ResourceUtilizationResponse`, `MemberUtilizationDetail`, `SprintVelocityResponse`
    - Place in `Application/DTOs/TimeEntries/`
    - _Requirements: 1.1, 2.1, 3.1, 5.2, 9.2, 10.3, 11.1_

  - [x] 4.2 Create cost rate and time policy DTOs
    - Add `CreateCostRateRequest`, `UpdateCostRateRequest`, `CostRateResponse` in `Application/DTOs/CostRates/`
    - Add `UpdateTimePolicyRequest`, `TimePolicyResponse` in `Application/DTOs/TimePolicies/`
    - Add `CostSnapshotResponse` in `Application/DTOs/CostSnapshots/`
    - _Requirements: 6.1, 6.2, 8.1, 12.2_

  - [x] 4.3 Create FluentValidation validators
    - Add `CreateTimeEntryRequestValidator`: `DurationMinutes > 0`, `StoryId` required, `Date` not in future
    - Add `UpdateTimeEntryRequestValidator`, `TimerStartRequestValidator`, `RejectTimeEntryRequestValidator`
    - Add `CreateCostRateRequestValidator`: `HourlyRate > 0`, `RateType` in allowed values, conditional member/role fields
    - Add `UpdateCostRateRequestValidator`, `UpdateTimePolicyRequestValidator`: `RequiredHoursPerDay` in (0, 24], `MaxDailyHours >= RequiredHoursPerDay`
    - Place in `Application/Validators/`
    - _Requirements: 1.3, 6.7, 8.5, 8.6_

- [x] 5. Infrastructure layer — EF Core configuration
  - [x] 5.1 Add DbSet properties and entity configurations to WorkDbContext
    - Add `DbSet<TimeEntry>`, `DbSet<CostRate>`, `DbSet<TimePolicy>`, `DbSet<TimeApproval>`, `DbSet<CostSnapshot>`
    - Configure indexes: composite on `(OrganizationId, StoryId)`, `(OrganizationId, MemberId, Date)` for TimeEntry; unique filtered index on CostRate; unique on `(OrganizationId)` for TimePolicy; composite on `(ProjectId, PeriodStart, PeriodEnd)` for CostSnapshot
    - Apply standard org-scoped global query filter `FlgStatus == "A"`
    - _Requirements: 1.1, 6.6, 8.1, 12.1_

  - [x] 5.2 Create EF Core migration for new tables
    - Generate migration for TimeEntries, CostRates, TimePolicies, TimeApprovals, CostSnapshots tables
    - _Requirements: 1.1, 6.1, 8.1, 5.1, 12.1_

- [x] 6. Infrastructure layer — repositories
  - [x] 6.1 Implement TimeEntryRepository
    - Implement `ITimeEntryRepository` in `Infrastructure/Repositories/TimeEntries/`
    - Include `GetDailyTotalMinutesAsync` for policy enforcement, `GetApprovedBillableByProjectAsync` for cost calculations, `GetApprovedBySprintAsync` for velocity
    - Paginated list with multi-filter support
    - _Requirements: 1.1, 4.1, 4.2, 4.4, 9.1, 11.1, 14.3_

  - [x] 6.2 Implement CostRateRepository
    - Implement `ICostRateRepository` in `Infrastructure/Repositories/CostRates/`
    - Include `ExistsDuplicateAsync` for duplicate detection, `GetActiveRatesForMemberAsync` and `GetActiveRatesForRoleDepartmentAsync` for rate resolution
    - _Requirements: 6.1, 6.4, 6.6, 7.1_

  - [x] 6.3 Implement TimePolicyRepository, TimeApprovalRepository, CostSnapshotRepository
    - Implement `ITimePolicyRepository` in `Infrastructure/Repositories/TimePolicies/`
    - Implement `ITimeApprovalRepository` in `Infrastructure/Repositories/TimeApprovals/`
    - Implement `ICostSnapshotRepository` with `AddOrUpdateAsync` upsert in `Infrastructure/Repositories/CostSnapshots/`
    - _Requirements: 8.1, 5.1, 12.1_

- [ ] 7. Infrastructure layer — CostRateResolver (pure function)
  - [x] 7.1 Implement CostRateResolver
    - Implement `ICostRateResolver` in `Infrastructure/Services/CostRates/CostRateResolver.cs`
    - Pure function: member rate → role+department rate → org default, picking most recent `effectiveFrom <= entryDate`
    - Returns 0 when no rate found
    - No database calls, no side effects
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [ ] 7.2 Write property test: Cost rate resolution follows precedence hierarchy (Property 15)
    - **Property 15: Cost rate resolution follows precedence hierarchy**
    - **Validates: Requirements 7.1, 7.2, 7.4**
    - Create `WorkService.Tests/Properties/CostRateResolverProperties.cs`
    - Create custom `Arbitrary<CostRate>` generator in `WorkService.Tests/Generators/CostRateGenerators.cs`
    - Test that member rate always wins over role+dept and org default; role+dept wins over org default; most recent effectiveFrom is selected; same inputs always produce same output

- [x] 8. Checkpoint — Ensure domain, application, and infrastructure layers compile
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Infrastructure layer — core services
  - [x] 9.1 Implement TimeEntryService
    - Implement `ITimeEntryService` in `Infrastructure/Services/TimeEntries/TimeEntryService.cs`
    - Create: validate story exists, check daily hours against policy, flag overtime, set status based on approval policy, resolve cost rate, publish activity log to outbox
    - Update: enforce ownership (or OrgAdmin), reset status to Pending if was Approved
    - Delete: enforce ownership, soft-delete
    - List: delegate to repository with filters
    - Approve/Reject: check approver role against policy workflow, create TimeApproval record, publish notification
    - GetProjectCostSummary: aggregate approved billable entries × resolved rates, group by member and department
    - GetProjectUtilization: calculate (loggedHours / expectedHours) × 100 per member
    - GetSprintVelocity: sum completed story points + approved hours, compute averageHoursPerPoint
    - _Requirements: 1.1, 1.2, 1.4, 1.5, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 4.1, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 11.1, 11.2, 11.3, 13.1, 13.3, 14.1, 14.2, 14.3_

  - [ ] 9.2 Write property tests for time entry creation (Properties 1, 2)
    - **Property 1: Time entry creation preserves all fields and applies defaults**
    - **Property 2: Non-positive duration is always rejected**
    - **Validates: Requirements 1.1, 1.3, 1.4, 13.1, 13.2**
    - Create `WorkService.Tests/Properties/TimeEntryCreationProperties.cs`
    - Create `WorkService.Tests/Generators/TimeEntryGenerators.cs`

  - [ ] 9.3 Write property tests for ownership and update behavior (Properties 6, 7, 8)
    - **Property 6: Time entry ownership enforcement**
    - **Property 7: Updating an approved entry resets status to Pending**
    - **Property 8: Soft-delete sets FlgStatus to D**
    - **Validates: Requirements 3.2, 3.3, 3.4, 3.5**
    - Create `WorkService.Tests/Properties/OwnershipProperties.cs`

  - [ ] 9.4 Write property test for list filtering (Property 9)
    - **Property 9: List filtering returns only matching entries and is ordered by date descending**
    - **Validates: Requirements 4.1, 4.2, 4.4, 13.4**
    - Create `WorkService.Tests/Properties/ListFilterProperties.cs`

  - [ ] 9.5 Write property tests for approval workflow (Properties 10, 11)
    - **Property 10: Approval and rejection create audit records**
    - **Property 11: Approval authorization enforcement**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5, 5.6**
    - Create `WorkService.Tests/Properties/ApprovalProperties.cs`

  - [ ] 9.6 Write property tests for project cost and utilization (Properties 17, 18, 19)
    - **Property 17: Project cost calculation is correct and order-independent**
    - **Property 18: Project cost date filtering excludes out-of-range entries**
    - **Property 19: Utilization calculation is correct**
    - **Validates: Requirements 9.1, 9.2, 9.3, 9.5, 10.1, 10.2, 10.3, 13.3**
    - Create `WorkService.Tests/Properties/ProjectCostProperties.cs`
    - Create `WorkService.Tests/Properties/UtilizationProperties.cs`

  - [ ] 9.7 Write property test for sprint velocity (Property 20)
    - **Property 20: Sprint velocity enrichment includes time data**
    - **Validates: Requirements 11.1**
    - Create `WorkService.Tests/Properties/VelocityProperties.cs`

  - [ ] 9.8 Write property test for daily hours policy enforcement (Property 22)
    - **Property 22: Daily hours policy enforcement**
    - **Validates: Requirements 14.1, 14.2, 14.3**
    - Create `WorkService.Tests/Properties/DailyHoursProperties.cs`

- [x] 10. Infrastructure layer — CostRateService and TimePolicyService
  - [x] 10.1 Implement CostRateService
    - Implement `ICostRateService` in `Infrastructure/Services/CostRates/CostRateService.cs`
    - Create: validate no duplicate, enforce OrgAdmin, validate hourly rate > 0
    - Update: enforce OrgAdmin, validate rate
    - Delete: enforce OrgAdmin, soft-delete
    - List: delegate to repository with filters
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_

  - [ ] 10.2 Write property tests for cost rate CRUD (Properties 12, 13, 14)
    - **Property 12: Cost rate CRUD with OrgAdmin restriction**
    - **Property 13: Duplicate cost rate rejection**
    - **Property 14: Non-positive hourly rate is always rejected**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7**
    - Create `WorkService.Tests/Properties/CostRateCrudProperties.cs`

  - [x] 10.3 Implement TimePolicyService
    - Implement `ITimePolicyService` in `Infrastructure/Services/TimePolicies/TimePolicyService.cs`
    - Get: return org policy or default values if none configured
    - Upsert: enforce OrgAdmin, validate fields, create or update
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

  - [ ] 10.4 Write property test for time policy (Property 16)
    - **Property 16: Time policy CRUD with validation**
    - **Validates: Requirements 8.1, 8.2, 8.4, 8.5, 8.6**
    - Create `WorkService.Tests/Properties/TimePolicyProperties.cs`
    - Create `WorkService.Tests/Generators/TimePolicyGenerators.cs`

- [x] 11. Infrastructure layer — TimerSessionService (Redis)
  - [x] 11.1 Implement TimerSessionService
    - Implement `ITimerSessionService` in `Infrastructure/Services/TimerSessions/TimerSessionService.cs`
    - Start: check no active timer via Redis SCAN `timer:{userId}:*`, SET with 24h TTL
    - Stop: SCAN for active timer, GET + DEL, calculate elapsed minutes, delegate to `TimeEntryService.CreateFromTimerAsync`
    - Status: return active session details or null
    - Handle Redis unavailability with 503 `SERVICE_UNAVAILABLE`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ] 11.2 Write property tests for timer (Properties 3, 4, 5)
    - **Property 3: Timer start/stop round trip produces a valid time entry**
    - **Property 4: Only one active timer per member**
    - **Property 5: Timer status reflects current state**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4**
    - Create `WorkService.Tests/Properties/TimerRoundTripProperties.cs`

- [x] 12. Infrastructure layer — CostSnapshotHostedService
  - [x] 12.1 Implement CostSnapshotHostedService
    - Implement as `IHostedService` with periodic timer (default 6 hours) in `Infrastructure/Services/CostSnapshots/CostSnapshotHostedService.cs`
    - On each tick: query active projects, compute cost summary per project, upsert CostSnapshot via `AddOrUpdateAsync`
    - Idempotent: re-running for same period produces identical results
    - Transaction per project to prevent partial snapshots
    - Also implement `ICostSnapshotService` for listing snapshots via API
    - _Requirements: 12.1, 12.4_

  - [ ] 12.2 Write property test for snapshot idempotence (Property 21)
    - **Property 21: Cost snapshot idempotence**
    - **Validates: Requirements 12.1, 12.4**
    - Create `WorkService.Tests/Properties/SnapshotProperties.cs`

- [x] 13. Checkpoint — Ensure all services compile and existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 14. API layer — controllers
  - [x] 14.1 Create TimeEntryController
    - Add `TimeEntryController` at `api/v1/time-entries` in `WorkService.Api/Controllers/`
    - Endpoints: POST `/` (create), GET `/` (list), PUT `/{timeEntryId}` (update), DELETE `/{timeEntryId}` (delete), POST `/{timeEntryId}/approve` (DeptLead), POST `/{timeEntryId}/reject` (DeptLead), POST `/timer/start`, POST `/timer/stop`, GET `/timer/status`
    - Use `ApiResponse<T>` envelope, extract orgId/userId/role/deptId from HttpContext
    - _Requirements: 1.1, 2.1, 2.2, 2.4, 3.1, 3.4, 4.1, 5.1, 5.2_

  - [x] 14.2 Create CostRateController
    - Add `CostRateController` at `api/v1/cost-rates` in `WorkService.Api/Controllers/`
    - Endpoints: POST `/` (OrgAdmin), GET `/`, PUT `/{costRateId}` (OrgAdmin), DELETE `/{costRateId}` (OrgAdmin)
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 14.3 Create TimePolicyController
    - Add `TimePolicyController` at `api/v1/time-policies` in `WorkService.Api/Controllers/`
    - Endpoints: GET `/`, PUT `/` (OrgAdmin)
    - _Requirements: 8.1, 8.2, 8.4_

  - [x] 14.4 Add cost/utilization/velocity endpoints to existing controllers
    - Add `GET /api/v1/projects/{projectId}/cost-summary` to ProjectController
    - Add `GET /api/v1/projects/{projectId}/utilization` to ProjectController
    - Add `GET /api/v1/projects/{projectId}/cost-snapshots` to ProjectController
    - Add `GET /api/v1/sprints/{sprintId}/velocity` to SprintController
    - _Requirements: 9.1, 10.1, 11.1, 12.2_

  - [ ] 14.5 Write property test for error envelope (Property 23)
    - **Property 23: Error responses use standard envelope**
    - **Validates: Requirements 15.2**
    - Create `WorkService.Tests/Properties/ErrorEnvelopeProperties.cs`

- [x] 15. DI registration and wiring
  - [x] 15.1 Register all new repositories, services, and hosted service in DependencyInjection.cs
    - Add scoped registrations for all 5 repositories, 6 services, and `CostRateResolver`
    - Add `AddHostedService<CostSnapshotHostedService>()` for background snapshot generation
    - _Requirements: 1.1, 6.1, 7.1, 8.1, 12.1_

- [x] 16. Final checkpoint — Ensure all tests pass and application compiles
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design (23 properties total)
- Error codes are remapped to 4050–4056 to avoid collision with existing Project codes (4041–4046)
- The CostRateResolver is a pure function and the most critical piece to test — Property 15 should be prioritized
