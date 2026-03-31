# Implementation Plan: Analytics & Reporting

## Overview

Implements the analytics and reporting layer (Phase 2) for WorkService, adding four domain entities (`VelocitySnapshot`, `ProjectHealthSnapshot`, `ResourceAllocationSnapshot`, `RiskRegister`), repository and service layers, two API controllers, a background snapshot hosted service, and integration with the existing `SprintService.CompleteAsync`. Tasks are ordered by dependency: domain → repositories → pure calculators → services → controllers → snapshot integration → tests → frontend.

## Tasks

- [x] 1. Domain entities, exceptions, and error codes
  - [x] 1.1 Create domain entities
    - Create `VelocitySnapshot.cs` in `Domain/Entities/VelocitySnapshots/`
    - Create `ProjectHealthSnapshot.cs` in `Domain/Entities/ProjectHealthSnapshots/`
    - Create `ResourceAllocationSnapshot.cs` in `Domain/Entities/ResourceAllocationSnapshots/`
    - Create `RiskRegister.cs` in `Domain/Entities/RiskRegisters/`
    - All entities implement `IOrganizationEntity`; `RiskRegister` includes `FlgStatus` for soft-delete
    - _Requirements: 1.2, 1.3, 3.2, 3.5, 5.1, 6.1_

  - [x] 1.2 Create domain exception classes
    - Create `InvalidAnalyticsParameterException.cs` (4060), `InvalidRiskSeverityException.cs` (4061), `InvalidRiskLikelihoodException.cs` (4062), `InvalidMitigationStatusException.cs` (4063), `RiskNotFoundException.cs` (4064), `SnapshotGenerationFailedException.cs` (4065) in `Domain/Exceptions/`
    - Each extends `DomainException` with the appropriate error code and HTTP status
    - _Requirements: 11.1_

  - [x] 1.3 Add error codes to ErrorCodes.cs
    - Add constants for `INVALID_ANALYTICS_PARAMETER` (4060) through `SNAPSHOT_GENERATION_FAILED` (4065)
    - Register mappings in `ErrorCodeResolverService`
    - _Requirements: 11.1, 11.2_

- [ ] 2. Repository interfaces and implementations
  - [x] 2.1 Create repository interfaces in Domain layer
    - Create `IRiskRegisterRepository.cs` in `Domain/Interfaces/Repositories/RiskRegisters/`
    - Create `IVelocitySnapshotRepository.cs` in `Domain/Interfaces/Repositories/VelocitySnapshots/`
    - Create `IProjectHealthSnapshotRepository.cs` in `Domain/Interfaces/Repositories/ProjectHealthSnapshots/`
    - Create `IResourceAllocationSnapshotRepository.cs` in `Domain/Interfaces/Repositories/ResourceAllocationSnapshots/`
    - Follow existing subfolder convention matching Infrastructure layout
    - _Requirements: 1.1, 3.3, 5.1, 5.9, 6.1, 6.3, 6.4_

  - [x] 2.2 Add EF Core configuration to WorkDbContext
    - Add four `DbSet<>` properties for the new entities
    - Configure entity mappings in `OnModelCreating`: primary keys, indexes, unique constraints, global query filters (org-scoped, `FlgStatus` filter for RiskRegister)
    - Key indexes: `VelocitySnapshot(ProjectId, SprintId)` unique, `ProjectHealthSnapshot(ProjectId, SnapshotDate)`, `ResourceAllocationSnapshot(ProjectId, MemberId, PeriodStart, PeriodEnd)` unique, `RiskRegister(OrganizationId, ProjectId)` filtered
    - _Requirements: 1.6, 3.6, 6.3, 9.4_

  - [x] 2.3 Create EF Core migration
    - Generate migration for the four new tables with all indexes and constraints
    - _Requirements: 1.1, 3.5, 5.1, 6.1_

  - [x] 2.4 Implement repository classes in Infrastructure layer
    - Create `RiskRegisterRepository.cs` in `Infrastructure/Repositories/RiskRegisters/`
    - Create `VelocitySnapshotRepository.cs` in `Infrastructure/Repositories/VelocitySnapshots/`
    - Create `ProjectHealthSnapshotRepository.cs` in `Infrastructure/Repositories/ProjectHealthSnapshots/`
    - Create `ResourceAllocationSnapshotRepository.cs` in `Infrastructure/Repositories/ResourceAllocationSnapshots/`
    - `VelocitySnapshotRepository.AddOrUpdateAsync` upserts on `(ProjectId, SprintId)` for idempotence
    - `ResourceAllocationSnapshotRepository.AddOrUpdateAsync` upserts on `(ProjectId, MemberId, PeriodStart, PeriodEnd)`
    - _Requirements: 1.6, 3.3, 3.4, 5.9, 6.1, 6.3, 6.4, 9.4_

- [x] 3. Checkpoint — Domain and data layer
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Application layer DTOs and validators
  - [x] 4.1 Create analytics DTOs
    - Create all DTOs in `Application/DTOs/Analytics/`: `VelocitySnapshotResponse`, `ResourceManagementResponse`, `ResourceUtilizationDetailResponse`, `ProjectCostAnalyticsResponse`, `ProjectHealthResponse`, `HealthScoreResult`, `DependencyAnalysisResponse`, `DependencyChain`, `BlockedStoryDetail`, `BugMetricsResponse`, `BugTrendItem`, `DashboardSummaryResponse`, `SnapshotStatusResponse`, `ProjectBreakdownItem`
    - _Requirements: 1.2, 2.2, 3.2, 4.1, 5.1, 7.1, 7.3, 8.1, 10.1_

  - [x] 4.2 Create risk register DTOs
    - Create `CreateRiskRequest.cs`, `UpdateRiskRequest.cs`, `RiskRegisterResponse.cs` in `Application/DTOs/RiskRegisters/`
    - _Requirements: 6.1, 6.2_

  - [x] 4.3 Create FluentValidation validators
    - Create `CreateRiskRequestValidator.cs` and `UpdateRiskRequestValidator.cs` in `Application/Validators/`
    - Validate `Title` required (max 200), `ProjectId` required, `Severity` in [Low, Medium, High, Critical], `Likelihood` in [Low, Medium, High], `MitigationStatus` in [Open, Mitigating, Mitigated, Accepted]
    - _Requirements: 6.5, 6.6, 6.7_

- [ ] 5. Service interfaces and pure calculators
  - [x] 5.1 Create service interfaces in Domain layer
    - Create `IAnalyticsService.cs` in `Domain/Interfaces/Services/Analytics/`
    - Create `IHealthScoreCalculator.cs` in `Domain/Interfaces/Services/Analytics/`
    - Create `IDependencyAnalyzer.cs` in `Domain/Interfaces/Services/Analytics/`
    - Create `IAnalyticsSnapshotService.cs` in `Domain/Interfaces/Services/Analytics/`
    - Create `IRiskRegisterService.cs` in `Domain/Interfaces/Services/RiskRegisters/`
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 8.1, 9.1, 10.1_

  - [x] 5.2 Implement HealthScoreCalculator
    - Create `HealthScoreCalculator.cs` in `Infrastructure/Services/Analytics/`
    - Implement `Calculate()`: velocityScore from last 3 sprints' completed/committed ratio, bugRateScore = max(0, 100 − bugRate), overdueScore = max(0, 100 − overdueRate), riskScore with severity weights (Critical=4, High=3, Medium=2, Low=1) normalized against maxExpectedRisk=20
    - Implement `DetermineTrend()`: improving if current > previous + 5, declining if current < previous − 5, stable otherwise
    - Neutral score of 50 when data is missing (100 for riskScore when no risks)
    - overallScore = velocityScore × 0.30 + bugRateScore × 0.25 + overdueScore × 0.25 + riskScore × 0.20
    - _Requirements: 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.10_

  - [ ]* 5.3 Write property tests for HealthScoreCalculator
    - **Property 12: Health sub-score calculations**
    - **Property 13: Health overall score weighted average**
    - **Property 14: Health trend determination**
    - **Validates: Requirements 5.2, 5.3, 5.4, 5.5, 5.6, 5.7**

  - [x] 5.4 Implement DependencyAnalyzer
    - Create `DependencyAnalyzer.cs` in `Infrastructure/Services/Analytics/`
    - Build adjacency list from StoryLink records (`blocks` → edge from source to target, `is_blocked_by` → edge from target to source)
    - DFS from root nodes (no incoming edges) to find maximal blocking chains
    - DFS coloring (WHITE/GRAY/BLACK) for cycle detection
    - Identify blocked stories: stories with incoming `is_blocked_by` where blocker status ≠ Done
    - Support `sprintId` filter to restrict analysis to sprint stories
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

  - [ ]* 5.5 Write property tests for DependencyAnalyzer
    - **Property 21: Dependency chain identification**
    - **Property 22: Blocked story identification**
    - **Property 23: Circular dependency detection**
    - **Property 24: Dependency sprint filtering**
    - **Validates: Requirements 7.2, 7.3, 7.4, 7.6, 7.7**

- [x] 6. Checkpoint — Pure calculators
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 7. RiskRegisterService implementation
  - [x] 7.1 Implement RiskRegisterService
    - Create `RiskRegisterService.cs` in `Infrastructure/Services/RiskRegisters/`
    - Implement CRUD: `CreateAsync` (validate enums, set FlgStatus=A, return 201), `UpdateAsync` (validate enums, update fields), `DeleteAsync` (soft-delete via FlgStatus=D), `ListAsync` (paginated with severity/mitigationStatus/sprintId filters)
    - Throw `InvalidRiskSeverityException` (4061), `InvalidRiskLikelihoodException` (4062), `InvalidMitigationStatusException` (4063) for invalid enums
    - Throw `RiskNotFoundException` (4064) for non-existent riskId
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.9_

  - [ ]* 7.2 Write property tests for RiskRegisterService
    - **Property 16: Risk creation preserves all fields**
    - **Property 17: Risk soft-delete excludes from queries**
    - **Property 18: Risk list filtering**
    - **Property 19: Risk enum validation**
    - **Validates: Requirements 6.1, 6.3, 6.4, 6.5, 6.6, 6.7**

- [ ] 8. AnalyticsService implementation
  - [x] 8.1 Implement velocity analytics methods
    - In `AnalyticsService.cs` (`Infrastructure/Services/Analytics/`), implement `GetVelocityTrendsAsync` (query VelocitySnapshotRepository, validate sprintCount 1–50, throw 4060 if invalid) and `GenerateVelocitySnapshotAsync` (compute committed/completed points, total logged hours, average hours per point, upsert snapshot)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

  - [ ]* 8.2 Write property tests for velocity analytics
    - **Property 1: Velocity query returns ordered snapshots with all required fields**
    - **Property 2: Invalid sprintCount is rejected**
    - **Property 28: Snapshot idempotence** (velocity portion)
    - **Validates: Requirements 1.1, 1.2, 1.5, 1.6, 9.4**

  - [x] 8.3 Implement resource management and utilization methods
    - Implement `GetResourceManagementAsync` (aggregate time entries by member, compute capacity utilization from TimePolicy, fetch member names from ProfileServiceClient, sort projectBreakdown by hours desc)
    - Implement `GetResourceUtilizationAsync` (per-project member utilization, serve from snapshot if available, fall back to real-time)
    - Implement `GenerateResourceAllocationSnapshotAsync` (compute and upsert per-member utilization for a project/period)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [ ]* 8.4 Write property tests for resource analytics
    - **Property 3: Resource management returns all members with complete allocation data**
    - **Property 4: Capacity utilization percentage formula**
    - **Property 5: Project breakdown is sorted by hours descending**
    - **Property 6: Resource utilization returns complete per-member data**
    - **Property 7: Snapshot-to-realtime consistency**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.6**

  - [x] 8.5 Implement project cost analytics methods
    - Implement `GetProjectCostAnalyticsAsync` (compute totalCost, billable/non-billable hours, burnRatePerDay = totalCost / workingDays, costByMember, costByDepartment, costTrend from CostSnapshot history)
    - Use existing `CostRateResolver` for cost calculations
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ]* 8.6 Write property tests for cost analytics
    - **Property 8: Cost analytics response completeness**
    - **Property 9: Burn rate calculation**
    - **Property 10: Cost trend ordering**
    - **Property 11: Cost calculation confluence**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.5**

  - [x] 8.7 Implement project health analytics methods
    - Implement `GetProjectHealthAsync` (return latest ProjectHealthSnapshot, support `history=true` for last 10 snapshots)
    - Implement `GenerateHealthSnapshotAsync` (fetch velocity snapshots, bug counts, overdue counts, active risks → delegate to HealthScoreCalculator → persist snapshot)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10_

  - [ ]* 8.8 Write property tests for health analytics
    - **Property 15: Health history ordering and limit**
    - **Property 27: Sprint-close triggers all snapshot types**
    - **Validates: Requirements 5.9, 9.1**

  - [x] 8.9 Implement bug metrics methods
    - Implement `GetBugMetricsAsync` (count total/open/closed/reopened bugs, compute bugRate, bugsBySeverity breakdown, bugTrend for last 10 completed sprints)
    - Scope by sprintId when provided via SprintStory records
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6_

  - [ ]* 8.10 Write property tests for bug metrics
    - **Property 25: Bug metrics response completeness and scoping**
    - **Property 26: Bug trend ordering**
    - **Validates: Requirements 8.1, 8.3, 8.4, 8.5**

  - [x] 8.11 Implement dashboard summary method
    - Implement `GetDashboardAsync` (aggregate latest health snapshot, latest velocity snapshot, active bug/risk/blocked counts, total cost, burn rate; serve from snapshots where available, fall back to real-time; cache in Redis with 5-minute TTL)
    - _Requirements: 10.1, 10.2, 10.3_

  - [ ]* 8.12 Write property tests for dashboard
    - **Property 29: Dashboard response completeness**
    - **Validates: Requirements 10.1**

- [x] 9. Checkpoint — Services complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 10. API controllers
  - [x] 10.1 Create AnalyticsController
    - Create `AnalyticsController.cs` in `Api/Controllers/`
    - Route: `api/v1/analytics`
    - Endpoints: GET `/velocity`, `/resource-management`, `/resource-utilization`, `/project-cost`, `/project-health`, `/dependencies`, `/bugs`, `/dashboard`, `/snapshot-status` (DeptLead)
    - Use standard `ApiResponse<T>` envelope with `CorrelationId`
    - Inject `IAnalyticsService` and `IDependencyAnalyzer`
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 7.1, 8.1, 9.5, 10.1, 11.2_

  - [x] 10.2 Create RiskRegisterController
    - Create `RiskRegisterController.cs` in `Api/Controllers/`
    - Route: `api/v1/analytics/risks`
    - Endpoints: POST `/` (DeptLead), PUT `/{riskId}` (DeptLead), DELETE `/{riskId}` (DeptLead), GET `/`
    - Use standard `ApiResponse<T>` envelope with `CorrelationId`
    - Inject `IRiskRegisterService`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.8, 11.2_

  - [ ]* 10.3 Write property test for role-based access control
    - **Property 20: Risk role-based access control**
    - **Validates: Requirements 6.8**

- [ ] 11. Snapshot integration with SprintService
  - [x] 11.1 Implement AnalyticsSnapshotHostedService
    - Create `AnalyticsSnapshotHostedService.cs` in `Infrastructure/Services/Analytics/`
    - Implement `IAnalyticsSnapshotService` and `IHostedService`
    - `TriggerSprintCloseSnapshotsAsync`: generate velocity, health, and resource allocation snapshots for the completed sprint's project
    - `GeneratePeriodicSnapshotsAsync`: iterate all active projects, generate health and resource snapshots, log errors per project and continue
    - Periodic timer: configurable interval (default 6 hours)
    - Store snapshot status metadata in Redis (`analytics:snapshot_status`, 24h TTL)
    - Implement `GetSnapshotStatusAsync` to read status from Redis
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 11.2 Integrate snapshot trigger into SprintService.CompleteAsync
    - Inject `IAnalyticsSnapshotService` into `SprintService`
    - After sprint completion logic, call `TriggerSprintCloseSnapshotsAsync(sprintId)` fire-and-forget with error logging
    - Snapshot failure must not block sprint completion
    - _Requirements: 9.1, 9.3_

  - [ ]* 11.3 Write property test for snapshot trigger
    - **Property 28: Snapshot idempotence** (full integration)
    - **Validates: Requirements 1.6, 9.4**

- [ ] 12. DI registration and wiring
  - [x] 12.1 Update DependencyInjection.cs
    - Register all four new repositories (scoped)
    - Register `IAnalyticsService`, `IHealthScoreCalculator`, `IDependencyAnalyzer`, `IRiskRegisterService`, `IAnalyticsSnapshotService` (scoped)
    - Register `AnalyticsSnapshotHostedService` as hosted service
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 8.1, 9.1, 10.1_

- [x] 13. Checkpoint — Backend complete
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 14. Frontend — Zustand stores and API layer
  - [x] 14.1 Create analytics Zustand store
    - Create `useAnalyticsStore` with actions for all analytics endpoints: velocity trends, resource management, resource utilization, project cost, project health, bug metrics, dashboard
    - Create `useRiskRegisterStore` with CRUD actions for risk register
    - Create `useDependencyStore` with action for dependency analysis
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 8.1, 10.1_

  - [x] 14.2 Create API service functions
    - Create typed API functions for all analytics and risk register endpoints
    - Use existing API client pattern with `ApiResponse<T>` unwrapping
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1, 8.1, 10.1_

- [ ] 15. Frontend — Analytics components
  - [x] 15.1 Create VelocityTrendChart component
    - Line chart showing committed vs completed points across sprints
    - Sprint count selector (1–50)
    - _Requirements: 1.1, 1.2_

  - [x] 15.2 Create ResourceManagementTable component
    - Table of member allocations with project breakdown expandable rows
    - Date range and department filters
    - _Requirements: 2.1, 2.2_

  - [x] 15.3 Create ResourceUtilizationChart component
    - Bar chart of per-member utilization percentages for a project
    - Date range filter
    - _Requirements: 3.1, 3.2_

  - [x] 15.4 Create ProjectCostDashboard component
    - Cost summary with burn rate, cost trend line chart, cost by member/department tables
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 15.5 Create ProjectHealthGauge component
    - Circular gauge for overall health score with sub-score breakdown
    - Trend indicator (improving/stable/declining)
    - History toggle for last 10 snapshots
    - _Requirements: 5.1, 5.7, 5.9_

  - [x] 15.6 Create RiskRegisterTable component
    - Paginated table with severity/status filters
    - Create and edit modals with form validation
    - Soft-delete confirmation
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [x] 15.7 Create DependencyGraph component
    - Visual graph of blocking chains using a graph library (e.g., react-flow)
    - Highlight critical path chains and circular dependencies
    - Sprint filter
    - _Requirements: 7.1, 7.2, 7.3, 7.6_

  - [x] 15.8 Create BugMetricsPanel component
    - Bug counts, bug rate, severity breakdown chart, trend chart
    - Sprint selector
    - _Requirements: 8.1, 8.5_

  - [x] 15.9 Create AnalyticsDashboard page
    - Consolidated view using the dashboard endpoint
    - Renders VelocityTrendChart, ProjectHealthGauge, BugMetricsPanel, RiskRegisterTable summary, DependencyGraph summary, cost summary
    - Project selector
    - _Requirements: 10.1, 10.2, 10.3_

- [x] 16. Final checkpoint — All tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document (29 total)
- Unit tests validate specific examples and edge cases
- The design uses C# (.NET 8) throughout — no language selection needed
- Pure calculators (HealthScoreCalculator, DependencyAnalyzer) are implemented and tested before the services that depend on them
