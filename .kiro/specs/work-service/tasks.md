# Implementation Plan: WorkService

## Overview

Incremental implementation of the WorkService microservice following Clean Architecture (.NET 8) with five projects: Domain, Application, Infrastructure, Api, and Tests. All projects live under `src/backend/WorkService/` in the Nexus-2.0 monorepo. Tasks build from the innermost layer (Domain) outward, wiring everything together in Program.cs at the end. All code is C# targeting `net8.0`.

WorkService is the largest service in the platform — managing projects, stories with professional IDs, tasks with department-based routing, sprints, board views, comments, labels, activity feeds, full-text search, reports, saved filters, story linking, and workflow customization. It has 13 entities, 46 error codes, 13 service interfaces, 12 repository interfaces, and 11 controllers.

## Tasks

- [x] 1. Solution and project scaffolding
  - [x] 1.1 Create monorepo folder structure and .NET 8 projects with project references
    - Create `src/backend/WorkService/` directory
    - Create `src/backend/WorkService/WorkService.Domain` (class library, net8.0, zero project references)
    - Create `src/backend/WorkService/WorkService.Application` (class library, net8.0, references Domain)
    - Create `src/backend/WorkService/WorkService.Infrastructure` (class library, net8.0, references Domain + Application)
    - Create `src/backend/WorkService/WorkService.Api` (web project, net8.0, references Application + Infrastructure)
    - Create `src/backend/WorkService/WorkService.Tests` (xUnit test project, net8.0, references Domain + Application + Infrastructure)
    - Add all five projects to `Nexus-2.0.sln`
    - _Requirements: 37.1, 37.2, 37.3, 37.4, 37.5_

  - [x] 1.2 Add NuGet package references to each project
    - Domain: no external packages
    - Application: `FluentValidation` only
    - Infrastructure: `Npgsql.EntityFrameworkCore.PostgreSQL`, `StackExchange.Redis`, `Microsoft.Extensions.Http.Polly`, `Polly`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`, `AspNetCore.HealthChecks.NpgSql`, `AspNetCore.HealthChecks.Redis`, `DotNetEnv`
    - Api: `FluentValidation.AspNetCore`, `Swashbuckle.AspNetCore`
    - Tests: `xunit`, `xunit.runner.visualstudio`, `FsCheck.Xunit`, `Moq`, `Microsoft.EntityFrameworkCore.InMemory`, `FluentAssertions`
    - _Requirements: 37.2, 37.3, 37.4, 37.5_

- [x] 2. Domain layer — Entities, exceptions, error codes, enums, helpers, interfaces
  - [x] 2.1 Create domain entities (13 entities including Project)
    - Implement `Project` entity with `ProjectId`, `OrganizationId`, `ProjectName`, `ProjectKey`, `Description`, `LeadId`, `FlgStatus`, `DateCreated`, `DateUpdated` — implements `IOrganizationEntity`
    - Implement `Story` entity with `StoryId`, `OrganizationId`, `ProjectId`, `StoryKey`, `SequenceNumber`, `Title`, `Description`, `AcceptanceCriteria`, `StoryPoints`, `Priority`, `Status`, `AssigneeId`, `ReporterId`, `SprintId`, `DepartmentId`, `DueDate`, `CompletedDate`, `FlgStatus`, `SearchVector`, `DateCreated`, `DateUpdated` — implements `IOrganizationEntity`
    - Implement `Task` entity with `TaskId`, `OrganizationId`, `StoryId`, `Title`, `Description`, `TaskType`, `Status`, `Priority`, `AssigneeId`, `DepartmentId`, `EstimatedHours`, `ActualHours`, `DueDate`, `CompletedDate`, `FlgStatus`, `SearchVector`, `DateCreated`, `DateUpdated` — implements `IOrganizationEntity`
    - Implement `Sprint` entity with `SprintId`, `OrganizationId`, `ProjectId`, `SprintName`, `Goal`, `StartDate`, `EndDate`, `Status`, `Velocity`, `DateCreated`, `DateUpdated` — implements `IOrganizationEntity`
    - Implement `SprintStory` entity with `SprintStoryId`, `SprintId`, `StoryId`, `AddedDate`, `RemovedDate`
    - Implement `Comment` entity with `CommentId`, `OrganizationId`, `EntityType`, `EntityId`, `AuthorId`, `Content`, `ParentCommentId`, `IsEdited`, `FlgStatus`, `DateCreated`, `DateUpdated` — implements `IOrganizationEntity`
    - Implement `ActivityLog` entity with `ActivityLogId`, `OrganizationId`, `EntityType`, `EntityId`, `StoryKey`, `Action`, `ActorId`, `ActorName`, `OldValue`, `NewValue`, `Description`, `DateCreated` — implements `IOrganizationEntity`
    - Implement `Label` entity with `LabelId`, `OrganizationId`, `Name`, `Color`, `DateCreated` — implements `IOrganizationEntity`
    - Implement `StoryLabel` entity with `StoryLabelId`, `StoryId`, `LabelId`
    - Implement `StoryLink` entity with `StoryLinkId`, `OrganizationId`, `SourceStoryId`, `TargetStoryId`, `LinkType`, `DateCreated` — implements `IOrganizationEntity`
    - Implement `StorySequence` entity with `ProjectId` (PK), `CurrentValue` (long, default 0) — NOT organization-scoped
    - Implement `SavedFilter` entity with `SavedFilterId`, `OrganizationId`, `TeamMemberId`, `Name`, `Filters` (JSON), `DateCreated` — implements `IOrganizationEntity`
    - Create `IOrganizationEntity` marker interface in `Common/` with `OrganizationId` property
    - _Requirements: 36.1, 36.2, 36.3, 36.4, 36.5, 36.6, 36.7, 36.8, 36.9, 36.10, 36.11, 36.12, 38.2_

  - [x] 2.2 Create `ErrorCodes` static class and `DomainException` base class
    - Implement `ErrorCodes` with all constants (1000, 4001–4046, 9999) and their string/int pairs
    - Implement `DomainException` base class with `ErrorValue`, `ErrorCode`, `StatusCode`, `CorrelationId`
    - _Requirements: 56.1, 39.1_

  - [x] 2.3 Create all concrete domain exception classes (4001–4046)
    - `StoryNotFoundException` (4001, 404), `TaskNotFoundException` (4002, 404), `SprintNotFoundException` (4003, 404), `InvalidStoryTransitionException` (4004, 400), `InvalidTaskTransitionException` (4005, 400), `SprintNotInPlanningException` (4006, 400), `StoryAlreadyInSprintException` (4007, 409), `StoryNotInSprintException` (4008, 400), `SprintOverlapException` (4009, 400), `LabelNotFoundException` (4010, 404), `LabelNameDuplicateException` (4011, 409), `CommentNotFoundException` (4012, 404), `StoryRequiresAssigneeException` (4013, 400), `StoryRequiresTasksException` (4014, 400), `StoryRequiresPointsException` (4015, 400), `OnlyOneActiveSprintException` (4016, 400), `CommentNotAuthorException` (4017, 403), `AssigneeNotInDepartmentException` (4018, 400), `AssigneeAtCapacityException` (4019, 400), `StoryKeyNotFoundException` (4020, 404), `SprintAlreadyActiveException` (4021, 400), `SprintAlreadyCompletedException` (4022, 400), `InvalidStoryPointsException` (4023, 400), `InvalidPriorityException` (4024, 400), `InvalidTaskTypeException` (4025, 400), `StoryInActiveSprintException` (4026, 400), `TaskInProgressException` (4027, 400), `SearchQueryTooShortException` (4028, 400), `MentionUserNotFoundException` (4029, 400), `OrganizationMismatchException` (4030, 403), `DepartmentAccessDeniedException` (4031, 403), `InsufficientPermissionsException` (4032, 403), `SprintEndBeforeStartException` (4033, 400), `StorySequenceInitFailedException` (4034, 500), `HoursMustBePositiveException` (4035, 400), `NotFoundException` (4036, 404), `ConflictException` (4037, 409), `ServiceUnavailableException` (4038, 503), `StoryDescriptionRequiredException` (4039, 400), `MaxLabelsPerStoryException` (4040, 400), `ProjectNotFoundException` (4041, 404), `ProjectNameDuplicateException` (4042, 409), `ProjectKeyDuplicateException` (4043, 409), `ProjectKeyImmutableException` (4044, 400), `ProjectKeyInvalidFormatException` (4045, 400), `StoryProjectMismatchException` (4046, 400), `RateLimitExceededException` (429, includes `RetryAfterSeconds`)
    - _Requirements: 56.1_

  - [x] 2.4 Create enums
    - `StoryStatus` (Backlog, Ready, InProgress, InReview, QA, Done, Closed)
    - `TaskStatus` (ToDo, InProgress, InReview, Done)
    - `TaskType` (Development, Testing, DevOps, Design, Documentation, Bug)
    - `Priority` (Critical, High, Medium, Low)
    - `SprintStatus` (Planning, Active, Completed, Cancelled)
    - `LinkType` (Blocks, IsBlockedBy, RelatesTo, Duplicates)
    - _Requirements: 4.2, 9.2, 7.1, 3.8, 12.1, 6.1_

  - [x] 2.5 Create helper classes
    - `WorkflowStateMachine` — static class with `StoryTransitions` and `TaskTransitions` dictionaries, `IsValidStoryTransition(from, to)`, `IsValidTaskTransition(from, to)`, `GetStoryTransitions()`, `GetTaskTransitions()`
    - `TaskTypeDepartmentMap` — static class mapping TaskType → DepartmentCode (Development→ENG, Testing→QA, DevOps→DEVOPS, Design→DESIGN, Documentation→PROD, Bug→ENG), `GetDepartmentCode(taskType)`, `GetAll()`
    - `DepartmentTypes` — static class with default department codes
    - _Requirements: 4.2, 4.3, 8.1, 9.2, 9.3, 32.4_

  - [x] 2.6 Create domain service interfaces (13 interfaces)
    - `IProjectService` (CreateAsync, GetByIdAsync, ListAsync, UpdateAsync, UpdateStatusAsync)
    - `IStoryService` (CreateAsync, GetByIdAsync, GetByKeyAsync, ListAsync, UpdateAsync, DeleteAsync, TransitionStatusAsync, AssignAsync, UnassignAsync, CreateLinkAsync, DeleteLinkAsync, ApplyLabelAsync, RemoveLabelAsync)
    - `ITaskService` (CreateAsync, GetByIdAsync, ListByStoryAsync, UpdateAsync, DeleteAsync, TransitionStatusAsync, AssignAsync, SelfAssignAsync, UnassignAsync, LogHoursAsync, SuggestAssigneeAsync)
    - `ISprintService` (CreateAsync, GetByIdAsync, ListAsync, UpdateAsync, StartAsync, CompleteAsync, CancelAsync, AddStoryAsync, RemoveStoryAsync, GetMetricsAsync, GetVelocityHistoryAsync, GetActiveSprintAsync)
    - `ICommentService` (CreateAsync, UpdateAsync, DeleteAsync, ListByEntityAsync)
    - `ILabelService` (CreateAsync, ListAsync, UpdateAsync, DeleteAsync)
    - `IActivityLogService` (LogAsync, GetByEntityAsync)
    - `ISearchService` (SearchAsync)
    - `IBoardService` (GetKanbanBoardAsync, GetSprintBoardAsync, GetBacklogAsync, GetDepartmentBoardAsync)
    - `IReportService` (GetVelocityChartAsync, GetDepartmentWorkloadAsync, GetCapacityUtilizationAsync, GetCycleTimeAsync, GetTaskCompletionAsync)
    - `IStoryIdGenerator` (GenerateNextIdAsync)
    - `IWorkflowService` (GetWorkflowsAsync, SaveOrganizationOverrideAsync, SaveDepartmentOverrideAsync)
    - `IOutboxService` (PublishAsync)
    - `IErrorCodeResolverService` (ResolveAsync)
    - _Requirements: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 44_

  - [x] 2.7 Create repository interfaces (12 interfaces)
    - `IProjectRepository` (GetByIdAsync, GetByKeyAsync, GetByNameAsync, AddAsync, UpdateAsync, ListAsync, GetStoryCountAsync, GetSprintCountAsync)
    - `IStoryRepository` (GetByIdAsync, GetByKeyAsync, AddAsync, UpdateAsync, ListAsync, SearchAsync, CountTasksAsync, CountCompletedTasksAsync, AllDevTasksDoneAsync, AllTasksDoneAsync, ExistsByProjectAsync)
    - `ITaskRepository` (GetByIdAsync, AddAsync, UpdateAsync, ListByStoryAsync, CountActiveByAssigneeAsync, ListBySprintAsync, ListByDepartmentAsync)
    - `ISprintRepository` (GetByIdAsync, AddAsync, UpdateAsync, ListAsync, GetActiveByProjectAsync, GetCompletedAsync, HasOverlappingAsync)
    - `ISprintStoryRepository` (GetAsync, AddAsync, UpdateAsync, ListBySprintAsync)
    - `ICommentRepository` (GetByIdAsync, AddAsync, UpdateAsync, ListByEntityAsync)
    - `IActivityLogRepository` (AddAsync, ListByEntityAsync)
    - `ILabelRepository` (GetByIdAsync, GetByNameAsync, AddAsync, UpdateAsync, RemoveAsync, ListAsync)
    - `IStoryLabelRepository` (GetAsync, AddAsync, RemoveAsync, CountByStoryAsync, ListByStoryAsync)
    - `IStoryLinkRepository` (GetByIdAsync, AddAsync, RemoveAsync, ListByStoryAsync, FindInverseAsync)
    - `IStorySequenceRepository` (InitializeAsync, IncrementAndGetAsync)
    - `ISavedFilterRepository` (GetByIdAsync, AddAsync, RemoveAsync, ListByMemberAsync)
    - _Requirements: 36.13, 36.14_

- [x] 3. Application layer — DTOs, validators, contracts
  - [x] 3.1 Create `ApiResponse<T>` envelope, `ErrorDetail`, and `PaginatedResponse<T>` classes
    - `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `ErrorValue`, `Message`, `CorrelationId`, `Errors`
    - `ErrorDetail` with `Field`, `Message`
    - `PaginatedResponse<T>` with `Data`, `TotalCount`, `Page`, `PageSize`, `TotalPages`
    - _Requirements: 40.1, 40.2, 40.3, 47.2_

  - [x] 3.2 Create request DTOs
    - Projects: `CreateProjectRequest`, `UpdateProjectRequest`, `ProjectStatusRequest`
    - Stories: `CreateStoryRequest`, `UpdateStoryRequest`, `StoryStatusRequest`, `StoryAssignRequest`, `CreateStoryLinkRequest`
    - Tasks: `CreateTaskRequest`, `UpdateTaskRequest`, `TaskStatusRequest`, `TaskAssignRequest`, `LogHoursRequest`
    - Sprints: `CreateSprintRequest`, `UpdateSprintRequest`, `AddStoryToSprintRequest`
    - Comments: `CreateCommentRequest`
    - Labels: `CreateLabelRequest`, `UpdateLabelRequest`, `ApplyLabelRequest`
    - Search: `SearchRequest`
    - SavedFilters: `CreateSavedFilterRequest`
    - Workflows: `WorkflowOverrideRequest`
    - _Requirements: 2, 3, 7, 10, 12, 14, 21, 23, 25, 26, 32, 34_

  - [x] 3.3 Create response DTOs
    - Projects: `ProjectDetailResponse`, `ProjectListResponse`
    - Stories: `StoryDetailResponse`, `StoryListResponse`, `DepartmentContribution`, `StoryLinkResponse`
    - Tasks: `TaskDetailResponse`, `SuggestAssigneeResponse`
    - Sprints: `SprintDetailResponse`, `SprintListResponse`, `SprintMetricsResponse`, `BurndownDataPoint`, `VelocityResponse`
    - Comments: `CommentResponse`
    - Labels: `LabelResponse`
    - Boards: `KanbanBoardResponse`, `KanbanColumn`, `KanbanCard`, `SprintBoardResponse`, `SprintBoardColumn`, `SprintBoardCard`, `BacklogResponse`, `BacklogItem`, `DepartmentBoardResponse`, `DepartmentBoardGroup`
    - Search: `SearchResponse`, `SearchResultItem`
    - Reports: `VelocityChartResponse`, `DepartmentWorkloadResponse`, `CapacityUtilizationResponse`, `CycleTimeResponse`, `TaskCompletionResponse`
    - SavedFilters: `SavedFilterResponse`
    - Workflows: `WorkflowDefinitionResponse`
    - Activity: `ActivityLogResponse`
    - _Requirements: 2.5, 2.6, 3.2, 3.3, 7.3, 8.2, 11.1, 12.3, 15.1, 17.3, 18.3, 19.2, 20.1, 21.7, 23.6, 24.2, 25.3, 26.2, 27.2, 28.1, 29.1, 30.1, 31.1, 32.1_

  - [x] 3.4 Create inter-service contract DTOs
    - `TeamMemberResponse` (Id, DisplayName, AvatarUrl, DepartmentId, MaxConcurrentTasks, Availability)
    - `DepartmentResponse` (DepartmentId, DepartmentCode, DepartmentName)
    - `OrganizationSettingsResponse` (DefaultSprintDurationWeeks)
    - `ErrorCodeResponse` (ResponseCode, Description)
    - _Requirements: 42.1, 55.1, 56.2_

  - [x] 3.5 Create FluentValidation validators for all request DTOs
    - `CreateProjectRequestValidator` (ProjectName required + max 200, ProjectKey required + regex `^[A-Z0-9]{2,10}$`)
    - `UpdateProjectRequestValidator` (ProjectName max 200 when present, ProjectKey regex when present)
    - `CreateStoryRequestValidator` (ProjectId required, Title required + max 200, Description max 5000, AcceptanceCriteria max 5000, StoryPoints Fibonacci when present, Priority must be Critical/High/Medium/Low)
    - `UpdateStoryRequestValidator` (Title max 200, Description max 5000, StoryPoints Fibonacci when present, Priority valid when present)
    - `StoryStatusRequestValidator` (Status required)
    - `CreateStoryLinkRequestValidator` (TargetStoryId required, LinkType required + must be blocks/is_blocked_by/relates_to/duplicates)
    - `CreateTaskRequestValidator` (StoryId required, Title required + max 200, Description max 3000, TaskType required + must be Development/Testing/DevOps/Design/Documentation/Bug, Priority valid, EstimatedHours > 0 when present)
    - `UpdateTaskRequestValidator` (Title max 200, Description max 3000, Priority valid when present, EstimatedHours > 0 when present)
    - `TaskStatusRequestValidator` (Status required)
    - `LogHoursRequestValidator` (Hours > 0)
    - `CreateSprintRequestValidator` (SprintName required + max 100, Goal max 500, StartDate required, EndDate required + > StartDate)
    - `UpdateSprintRequestValidator` (SprintName max 100 when present, Goal max 500 when present)
    - `AddStoryToSprintRequestValidator` (StoryId required)
    - `CreateCommentRequestValidator` (EntityType required + must be Story/Task, EntityId required, Content required)
    - `CreateLabelRequestValidator` (Name required + max 50, Color required + max 7 + hex regex)
    - `UpdateLabelRequestValidator` (Name max 50 when present, Color hex regex when present)
    - `SearchRequestValidator` (Query min 2 chars, PageSize 1–100, Page >= 1)
    - `CreateSavedFilterRequestValidator` (Name required + max 100, Filters required)
    - `WorkflowOverrideRequestValidator` (validate override structure)
    - _Requirements: 41.1, 41.2, 41.3, 2.2, 3.7, 3.8, 7.2, 10.2, 12.2, 25.2_

  - [x] 3.6 Create `OutboxMessage` class
    - `MessageId`, `MessageType`, `ServiceName` ("WorkService"), `OrganizationId`, `UserId`, `Action`, `EntityType`, `EntityId`, `OldValue`, `NewValue`, `IpAddress`, `CorrelationId`, `Timestamp`, `RetryCount`, `NotificationType`, `TemplateVariables`
    - _Requirements: 44.1, 44.2_

- [x] 4. Checkpoint — Verify Domain and Application layers compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Infrastructure layer — Data access (EF Core + PostgreSQL)
  - [x] 5.1 Create `WorkDbContext` with entity configurations, global query filters, and full-text search
    - Configure all 13 entities with PKs, indexes, unique constraints, max lengths, default values
    - Configure `Project`: PK `ProjectId`, unique index on `ProjectKey` (global), unique index on `(OrganizationId, ProjectName)`, `ProjectKey` max 10, `ProjectName` max 200
    - Configure `Story`: PK `StoryId`, unique index on `(ProjectId, StoryKey)`, indexes on `(OrganizationId, Status)`, `(OrganizationId, ProjectId)`, `(OrganizationId, SprintId)`, `(OrganizationId, AssigneeId)`, computed `SearchVector` tsvector column with GIN index (title weight A, description weight B)
    - Configure `Task`: PK `TaskId`, indexes on `(OrganizationId, StoryId)`, `(OrganizationId, AssigneeId)`, `(OrganizationId, DepartmentId)`, computed `SearchVector` tsvector column with GIN index
    - Configure `Sprint`: PK `SprintId`, index on `(OrganizationId, ProjectId, Status)`, FK to Project
    - Configure `SprintStory`: PK `SprintStoryId`, unique filtered index on `(SprintId, StoryId)` where `RemovedDate IS NULL`
    - Configure `Comment`: PK `CommentId`, index on `(EntityType, EntityId)`, FlgStatus filter
    - Configure `ActivityLog`: PK `ActivityLogId`, index on `(EntityType, EntityId)`
    - Configure `Label`: PK `LabelId`, unique index on `(OrganizationId, Name)`
    - Configure `StoryLabel`: PK `StoryLabelId`, unique index on `(StoryId, LabelId)`
    - Configure `StoryLink`: PK `StoryLinkId`, FKs to Story with `DeleteBehavior.Restrict`
    - Configure `StorySequence`: PK `ProjectId`, `CurrentValue` default 0 — NOT organization-scoped
    - Configure `SavedFilter`: PK `SavedFilterId`, index on `(OrganizationId, TeamMemberId)`, `Filters` as `jsonb`
    - Apply global query filters by `OrganizationId` on all entities implementing `IOrganizationEntity`
    - Apply `FlgStatus == "A"` query filters on Story, Task, Comment
    - Extract `organizationId` from `IHttpContextAccessor` for filter scoping
    - _Requirements: 36.1–36.14, 38.1, 38.2, 38.3, 48.2_

  - [x] 5.2 Create `DatabaseMigrationHelper` for auto-migration on startup
    - Apply pending EF Core migrations automatically
    - Use `EnsureCreated()` for InMemory database (test environment)
    - _Requirements: 45.1, 45.2, 45.3_

  - [x] 5.3 Implement all 12 repository classes
    - `ProjectRepository` — GetByIdAsync, GetByKeyAsync, GetByNameAsync, AddAsync, UpdateAsync, ListAsync (paginated, filterable by status), GetStoryCountAsync, GetSprintCountAsync
    - `StoryRepository` — GetByIdAsync, GetByKeyAsync, AddAsync, UpdateAsync, ListAsync (paginated, filterable by projectId/status/priority/department/assignee/sprint/labels/dateRange), SearchAsync (full-text tsvector/tsquery), CountTasksAsync, CountCompletedTasksAsync, AllDevTasksDoneAsync, AllTasksDoneAsync, ExistsByProjectAsync
    - `TaskRepository` — GetByIdAsync, AddAsync, UpdateAsync, ListByStoryAsync, CountActiveByAssigneeAsync, ListBySprintAsync, ListByDepartmentAsync
    - `SprintRepository` — GetByIdAsync, AddAsync, UpdateAsync, ListAsync (paginated, filterable by status/projectId), GetActiveByProjectAsync, GetCompletedAsync, HasOverlappingAsync
    - `SprintStoryRepository` — GetAsync, AddAsync, UpdateAsync, ListBySprintAsync
    - `CommentRepository` — GetByIdAsync, AddAsync, UpdateAsync, ListByEntityAsync (threaded)
    - `ActivityLogRepository` — AddAsync, ListByEntityAsync (sorted by date descending)
    - `LabelRepository` — GetByIdAsync, GetByNameAsync, AddAsync, UpdateAsync, RemoveAsync, ListAsync
    - `StoryLabelRepository` — GetAsync, AddAsync, RemoveAsync, CountByStoryAsync, ListByStoryAsync
    - `StoryLinkRepository` — GetByIdAsync, AddAsync, RemoveAsync, ListByStoryAsync, FindInverseAsync
    - `StorySequenceRepository` — InitializeAsync (INSERT ON CONFLICT DO NOTHING), IncrementAndGetAsync (atomic UPDATE RETURNING)
    - `SavedFilterRepository` — GetByIdAsync, AddAsync, RemoveAsync, ListByMemberAsync
    - _Requirements: 1.3, 1.4, 2.6, 3.3, 7.4, 12.4, 21.7, 24.2, 25.1, 25.7, 36.13, 36.14_

- [x] 6. Infrastructure layer — Configuration
  - [x] 6.1 Create `AppSettings` configuration class
    - `AppSettings.FromEnvironment()` loading from env vars via DotNetEnv
    - All configurable values: DB connection, Redis connection, JWT settings (SecretKey, Issuer, Audience), service URLs (ProfileService, SecurityService, UtilityService), allowed origins, service auth (ServiceId, ServiceName, ServiceSecret)
    - `GetRequired()` throws `InvalidOperationException` for missing required vars
    - _Requirements: 52.1, 52.2, 52.3_

  - [x] 6.2 Create `.env.example` with all required environment variables
    - Document all env vars with sensible defaults for local development
    - _Requirements: 52.1_

- [x] 7. Infrastructure layer — Redis services
  - [x] 7.1 Implement `OutboxService` (Redis LPUSH to `outbox:work`)
    - `PublishAsync` — serialize `OutboxMessage` to JSON, LPUSH to `outbox:work`
    - Retry up to 3 times with exponential backoff on failure
    - Move to `dlq:work` after 3 failures
    - _Requirements: 44.1, 44.2, 44.3, 44.4_

  - [x] 7.2 Implement `ErrorCodeResolverService`
    - Check in-memory `ConcurrentDictionary` first (fastest)
    - Check Redis cache at `error_code:{code}` (24-hour TTL)
    - Call UtilityService on cache miss
    - Fall back to static `MapErrorToResponseCode` mapping on failure
    - _Requirements: 56.2, 39.1_

- [x] 8. Infrastructure layer — Core services (Project, Story, Task, Sprint)
  - [x] 8.1 Implement `ProjectService`
    - `CreateAsync` — validate ProjectKey format (regex `^[A-Z0-9]{2,10}$`), validate ProjectKey global uniqueness, validate ProjectName uniqueness within org, restrict to OrgAdmin/DeptLead, create project with `FlgStatus=A`, return 201
    - `GetByIdAsync` — return project detail with story count, sprint count, lead info
    - `ListAsync` — paginated list filterable by status
    - `UpdateAsync` — validate name uniqueness if changed, enforce ProjectKey immutability when stories exist (check via `IStoryRepository.ExistsByProjectAsync`)
    - `UpdateStatusAsync` — update FlgStatus
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10, 2.11_

  - [x] 8.2 Implement `StoryService` with `StoryIdGenerator`
    - `StoryIdGenerator.GenerateNextIdAsync` — check Redis cache `project_prefix:{projectId}` (60-min TTL), look up ProjectKey on miss, initialize sequence row via INSERT ON CONFLICT DO NOTHING, atomic increment via UPDATE RETURNING
    - `CreateAsync` — validate project exists and belongs to org, generate story key via StoryIdGenerator, set status to Backlog, set ReporterId, create activity log entry, publish StoryCreated audit event, return 201
    - `GetByIdAsync` — return story detail with tasks, comments count, labels, activity log, links, completion percentage, department contributions
    - `GetByKeyAsync` — resolve story by key (e.g., `NEXUS-42`), return 404 `STORY_KEY_NOT_FOUND` if not found
    - `ListAsync` — paginated list filterable by projectId, status, priority, department, assignee, sprint, labels, date range
    - `UpdateAsync` — validate Fibonacci points, validate priority, record changes in activity log
    - `DeleteAsync` — soft delete, reject if story is in active sprint (`STORY_IN_ACTIVE_SPRINT`)
    - `TransitionStatusAsync` — validate against WorkflowStateMachine, enforce preconditions (description for Ready, points for Ready, assignee for InProgress, tasks for InReview, all tasks done for QA→Done), set CompletedDate on Done, create activity log, publish StoryStatusChanged notification
    - `AssignAsync` — set AssigneeId, enforce DeptLead department scope, publish StoryAssigned notification
    - `UnassignAsync` — clear AssigneeId, create activity log
    - `CreateLinkAsync` — create bidirectional link (blocks↔is_blocked_by, relates_to↔relates_to, duplicates↔duplicates)
    - `DeleteLinkAsync` — remove both directions
    - `ApplyLabelAsync` — enforce max 10 labels per story
    - `RemoveLabelAsync` — remove label from story
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 23.3, 23.4, 23.5, 33.1, 33.3_

  - [x] 8.3 Implement `TaskService`
    - `CreateAsync` — validate task type, auto-map department via TaskTypeDepartmentMap, set status to ToDo, create activity log, return 201
    - `GetByIdAsync` — return task detail with parent story key, assignee info, department, time tracking
    - `ListByStoryAsync` — return all tasks for a story
    - `UpdateAsync` — update task fields, record changes in activity log
    - `DeleteAsync` — soft delete, reject if task is InProgress (`TASK_IN_PROGRESS`)
    - `TransitionStatusAsync` — validate against WorkflowStateMachine, enforce assignee for ToDo→InProgress, set CompletedDate on Done, create activity log, publish TaskStatusChanged notification
    - `AssignAsync` — validate assignee is in target department, validate capacity (MaxConcurrentTasks), enforce DeptLead department scope
    - `SelfAssignAsync` — set AssigneeId to authenticated user, publish TaskAssigned notification
    - `UnassignAsync` — clear AssigneeId, create activity log
    - `LogHoursAsync` — validate hours > 0, add to ActualHours, record time entry
    - `SuggestAssigneeAsync` — find available member in mapped department with lowest active task count under MaxConcurrentTasks limit
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 11.1, 11.2, 11.3_

  - [x] 8.4 Implement `SprintService`
    - `CreateAsync` — validate project exists, validate EndDate > StartDate, set status to Planning, use org DefaultSprintDurationWeeks from ProfileService when not specified, return 201
    - `GetByIdAsync` — return sprint detail with project info, stories, metrics, burndown
    - `ListAsync` — paginated list filterable by status and projectId
    - `UpdateAsync` — only allow updates when sprint is in Planning status
    - `StartAsync` — transition Planning→Active, enforce one active sprint per project, cache `sprint_active:{projectId}`, publish SprintStarted notification
    - `CompleteAsync` — transition Active→Completed, calculate velocity (sum of Done/Closed story points), move incomplete stories back to Backlog (clear SprintId), invalidate caches, publish SprintEnded notification with velocity and completion rate
    - `CancelAsync` — transition to Cancelled, move all stories back to Backlog
    - `AddStoryAsync` — validate sprint in Planning, validate story belongs to same project as sprint, create SprintStory record, set story SprintId
    - `RemoveStoryAsync` — set RemovedDate on SprintStory, clear story SprintId
    - `GetMetricsAsync` — calculate TotalStories, CompletedStories, TotalStoryPoints, CompletedStoryPoints, CompletionRate, Velocity, StoriesByStatus, TasksByDepartment, BurndownData (ideal linear decrease vs actual), cache in Redis `sprint_metrics:{sprintId}` (5-min TTL)
    - `GetVelocityHistoryAsync` — return last N completed sprints with velocity data, sorted by end date descending
    - `GetActiveSprintAsync` — return current active sprint for org/project
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 15.1, 15.2, 15.3, 16.1, 16.2, 16.3_

- [x] 9. Infrastructure layer — Supporting services (Comment, Label, ActivityLog, Search, Board, Report, Workflow)
  - [x] 9.1 Implement `CommentService`
    - `CreateAsync` — create comment with AuthorId, support threaded replies via ParentCommentId, resolve @mentions (by displayName or email) via ProfileServiceClient, publish MentionedInComment notification with MentionerName/StoryKey/CommentPreview (first 100 chars), create activity log entry, return 201
    - `UpdateAsync` — only author can edit, set IsEdited=true, return 403 `COMMENT_NOT_AUTHOR` for non-authors
    - `DeleteAsync` — author or OrgAdmin can delete, soft delete (FlgStatus=D)
    - `ListByEntityAsync` — return threaded comments sorted by creation date
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.5, 21.6, 21.7, 21.8, 22.1, 22.2, 22.3_

  - [x] 9.2 Implement `LabelService`
    - `CreateAsync` — create organization-scoped label, validate name uniqueness within org, return 201
    - `ListAsync` — return all labels for the organization
    - `UpdateAsync` — update label name and color
    - `DeleteAsync` — delete label
    - _Requirements: 23.1, 23.2, 23.6, 23.7, 23.8, 23.9_

  - [x] 9.3 Implement `ActivityLogService`
    - `LogAsync` — create immutable ActivityLog entry with EntityType, EntityId, StoryKey, Action, ActorId, ActorName, OldValue, NewValue, Description, scoped to organization
    - `GetByEntityAsync` — return activity timeline sorted by date descending
    - Track action types: Created, StatusChanged, Assigned, Unassigned, PriorityChanged, PointsChanged, SprintAdded, SprintRemoved, LabelAdded, LabelRemoved, CommentAdded, DescriptionUpdated, DueDateChanged, TaskAdded, DepartmentChanged
    - _Requirements: 24.1, 24.2, 24.3, 24.4_

  - [x] 9.4 Implement `SearchService`
    - `SearchAsync` — full-text search using PostgreSQL tsvector/tsquery across story titles, descriptions, acceptance criteria, story keys, task titles, task descriptions
    - Validate query >= 2 characters (SEARCH_QUERY_TOO_SHORT)
    - Use weighted search: story keys and titles weight A, descriptions weight B
    - Return results with EntityType, StoryKey, Title, Status, Priority, AssigneeName, DepartmentName, relevance score
    - Support filtering by status, priority, department, assignee, sprint, labels, entityType, dateRange
    - Cache results in Redis `search_results:{hash}` (1-min TTL)
    - Scope results to authenticated user's organization
    - _Requirements: 25.1, 25.2, 25.3, 25.4, 25.5, 25.6, 25.7_

  - [x] 9.5 Implement `BoardService`
    - `GetKanbanBoardAsync` — return stories grouped by workflow status columns, support optional projectId/sprintId/departmentId/assigneeId/priority/labels filters, include KanbanCard with StoryKey/Title/Priority/StoryPoints/AssigneeName/Labels/TaskCount/CompletedTaskCount/ProjectName, cache in Redis `board_kanban:{orgId}:{projectId}:{sprintId}` (2-min TTL)
    - `GetSprintBoardAsync` — return active sprint's tasks grouped by task status (ToDo/InProgress/InReview/Done), support optional projectId, return empty board message when no active sprint, include SprintBoardCard with StoryKey/TaskTitle/TaskType/AssigneeName/DepartmentName/Priority/ProjectName
    - `GetBacklogAsync` — return stories where SprintId IS NULL, sorted by priority then DateCreated, support optional projectId, include TotalStories/TotalPoints/BacklogItems, cache in Redis `board_backlog:{orgId}:{projectId}` (2-min TTL)
    - `GetDepartmentBoardAsync` — return tasks grouped by department with task count/member count/tasks by status, support optional sprintId/projectId, cache in Redis `board_dept:{orgId}:{projectId}:{sprintId}` (2-min TTL)
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 18.1, 18.2, 18.3, 19.1, 19.2, 19.3, 20.1, 20.2_

  - [x] 9.6 Implement `ReportService`
    - `GetVelocityChartAsync` — return velocity data for last N completed sprints (default 10) with SprintName/Velocity/TotalStoryPoints/CompletionRate/StartDate/EndDate
    - `GetDepartmentWorkloadAsync` — return per-department metrics: DepartmentName/TotalTasks/CompletedTasks/InProgressTasks/MemberCount/AvgTasksPerMember, scope to sprint when sprintId provided
    - `GetCapacityUtilizationAsync` — return per-member metrics: MemberName/Department/ActiveTasks/MaxConcurrentTasks/UtilizationRate/Availability, filter by departmentId when provided
    - `GetCycleTimeAsync` — return cycle time for completed stories: StoryKey/Title/CycleTimeDays (InProgress→Done)/LeadTimeDays (Created→Done)/CompletedDate, filter by date range
    - `GetTaskCompletionAsync` — return per-department: DepartmentName/TotalTasks/CompletedTasks/CompletionRate/AvgCompletionTimeHours, filter by sprintId and date range
    - _Requirements: 27.1, 27.2, 28.1, 28.2, 29.1, 29.2, 30.1, 30.2, 31.1_

  - [x] 9.7 Implement `WorkflowService`
    - `GetWorkflowsAsync` — return default workflow definitions for stories and tasks (from WorkflowStateMachine)
    - `SaveOrganizationOverrideAsync` — save organization-level workflow overrides (custom status names, additional statuses, modified transitions)
    - `SaveDepartmentOverrideAsync` — save department-level workflow overrides
    - Workflow validation checks org-level overrides first, then falls back to defaults
    - _Requirements: 32.1, 32.2, 32.3, 32.4_

- [x] 10. Infrastructure layer — Service clients and DI registration
  - [x] 10.1 Create `IProfileServiceClient` and `ProfileServiceClient` typed client
    - `GetOrganizationSettingsAsync` — call ProfileService `GET /api/v1/organizations/{id}/settings` for sprint duration defaults
    - `GetTeamMemberAsync` — look up team member by ID
    - `GetDepartmentMembersAsync` — get members of a department for assignee suggestion
    - `GetDepartmentByCodeAsync` — look up department by code for task auto-mapping
    - `ResolveUserByDisplayNameAsync` — resolve @mention by display name
    - `ResolveUserByEmailAsync` — resolve @mention by email
    - Automatic service token refresh when within 30 seconds of expiry
    - Propagate `X-Organization-Id` header from current request context
    - _Requirements: 42.1, 50.1, 50.2, 50.3, 55.1_

  - [x] 10.2 Create `ISecurityServiceClient` and `SecurityServiceClient` typed client
    - `GetServiceTokenAsync` — call SecurityService `POST /api/v1/service-tokens/issue` for service JWT
    - Cache service token in Redis, auto-refresh when within 30 seconds of expiry
    - _Requirements: 42.2, 50.1, 50.2, 55.1_

  - [x] 10.3 Create `CorrelationIdDelegatingHandler`
    - Propagate `X-Correlation-Id` header on all outgoing HTTP calls
    - Propagate `X-Organization-Id` header when available
    - _Requirements: 42.6, 43.3_

  - [x] 10.4 Create `DependencyInjection` extension class for Infrastructure service registration
    - Register all 12 repositories
    - Register all 13+ services (ProjectService, StoryService, TaskService, SprintService, CommentService, LabelService, ActivityLogService, SearchService, BoardService, ReportService, WorkflowService, StoryIdGenerator, OutboxService, ErrorCodeResolverService)
    - Register typed HTTP clients (ProfileService, SecurityService) with Polly policies (3 retries exponential, circuit breaker 5/30s, 10s timeout)
    - Register Redis `IConnectionMultiplexer`
    - Register `WorkDbContext` with PostgreSQL
    - Register `IHttpContextAccessor`
    - Register `CorrelationIdDelegatingHandler`
    - _Requirements: 42.3, 42.5_

- [x] 11. Checkpoint — Verify Infrastructure layer compiles
  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Api layer — Middleware pipeline
  - [x] 12.1 Implement `CorrelationIdMiddleware`
    - Generate or propagate `X-Correlation-Id` header, store in `HttpContext.Items["CorrelationId"]`, include in response headers
    - _Requirements: 43.1, 43.2, 43.4_

  - [x] 12.2 Implement `GlobalExceptionHandlerMiddleware`
    - Catch `DomainException` → resolve via `IErrorCodeResolverService` → return `ApiResponse<object>` with `application/problem+json`
    - Catch `RateLimitExceededException` → add `Retry-After` header
    - Catch unhandled exceptions → return 500 `INTERNAL_ERROR`, no stack trace leakage, publish error event to `outbox:work`
    - _Requirements: 39.1, 39.2, 39.3, 39.4, 57.2, 57.3_

  - [x] 12.3 Implement `RateLimiterMiddleware`
    - Apply rate limiting on unauthenticated or high-traffic endpoints
    - _Requirements: 57.1_

  - [x] 12.4 Implement `JwtClaimsMiddleware`
    - Extract JWT claims (userId, organizationId, departmentId, roleName, departmentRole, deviceId, jti) and store in `HttpContext.Items`
    - _Requirements: 57.1_

  - [x] 12.5 Implement `TokenBlacklistMiddleware`
    - Check `blacklist:{jti}` in Redis for every authenticated request
    - Return 401 `TOKEN_REVOKED` if blacklisted
    - _Requirements: 57.1_

  - [x] 12.6 Implement `RoleAuthorizationMiddleware`
    - Extract `roleName` and `departmentId` from JWT claims
    - OrgAdmin → organization-wide access
    - DeptLead → department-scoped access (own department only)
    - Member/Viewer → enforce department access matrix
    - Return 403 `INSUFFICIENT_PERMISSIONS` or `DEPARTMENT_ACCESS_DENIED`
    - _Requirements: 57.1_

  - [x] 12.7 Implement `OrganizationScopeMiddleware`
    - Extract `organizationId` from JWT claims, validate against route/query params
    - Skip for service-auth tokens
    - Return 403 `ORGANIZATION_MISMATCH` on cross-org access
    - _Requirements: 38.3, 38.4, 57.1_

  - [x] 12.8 Create `OrgAdminAttribute`, `DeptLeadAttribute`, and `ServiceAuthAttribute` custom authorization attributes
    - `OrgAdminAttribute` — mark endpoints requiring OrgAdmin role
    - `DeptLeadAttribute` — mark endpoints requiring DeptLead or higher
    - `ServiceAuthAttribute` — validate service JWT on service-to-service endpoints
    - _Requirements: 2.9, 34.1_

  - [x] 12.9 Create `MiddlewarePipelineExtensions` to register middleware in correct order
    - CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope → Controllers
    - Note: WorkService does NOT include FirstTimeUserMiddleware — that is enforced by SecurityService
    - _Requirements: 57.1_

- [x] 13. Api layer — Controllers (11 controllers)
  - [x] 13.1 Implement `ProjectController`
    - `POST /api/v1/projects` — Bearer (OrgAdmin, DeptLead) — CreateProjectRequest → 201 ProjectDetailResponse
    - `GET /api/v1/projects` — Bearer — Paginated ProjectListResponse (filterable by status)
    - `GET /api/v1/projects/{id}` — Bearer — ProjectDetailResponse (story count, sprint count, lead info)
    - `PUT /api/v1/projects/{id}` — Bearer (OrgAdmin, DeptLead) — UpdateProjectRequest → ProjectDetailResponse
    - `PATCH /api/v1/projects/{id}/status` — Bearer (OrgAdmin) — ProjectStatusRequest → 200
    - All responses wrapped in `ApiResponse<T>` with CorrelationId
    - _Requirements: 2.1, 2.5, 2.6, 2.7, 2.8, 34.1_

  - [x] 13.2 Implement `StoryController`
    - `POST /api/v1/stories` — Bearer (OrgAdmin, DeptLead, Member) — CreateStoryRequest → 201 StoryDetailResponse
    - `GET /api/v1/stories` — Bearer — Paginated StoryListResponse (filterable by projectId, status, priority, department, assignee, sprint, labels, date range)
    - `GET /api/v1/stories/{id}` — Bearer — StoryDetailResponse (tasks, comments count, labels, activity, links, completion %)
    - `GET /api/v1/stories/by-key/{storyKey}` — Bearer — StoryDetailResponse by professional key
    - `PUT /api/v1/stories/{id}` — Bearer (OrgAdmin, DeptLead, Member) — UpdateStoryRequest → StoryDetailResponse
    - `DELETE /api/v1/stories/{id}` — Bearer (OrgAdmin, DeptLead) — Soft delete
    - `PATCH /api/v1/stories/{id}/status` — Bearer (Member+) — StoryStatusRequest → StoryDetailResponse
    - `PATCH /api/v1/stories/{id}/assign` — Bearer (DeptLead+) — StoryAssignRequest → StoryDetailResponse
    - `PATCH /api/v1/stories/{id}/unassign` — Bearer (DeptLead+) — 200
    - `POST /api/v1/stories/{id}/links` — Bearer (Member+) — CreateStoryLinkRequest → 201
    - `DELETE /api/v1/stories/{id}/links/{linkId}` — Bearer (Member+) — 200
    - `POST /api/v1/stories/{id}/labels` — Bearer (Member+) — ApplyLabelRequest → 200
    - `DELETE /api/v1/stories/{id}/labels/{labelId}` — Bearer (Member+) — 200
    - `GET /api/v1/stories/{id}/comments` — Bearer — List CommentResponse (threaded)
    - `GET /api/v1/stories/{id}/activity` — Bearer — List ActivityLogResponse
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 4.2, 5.1, 5.5, 6.1, 6.4, 23.3, 23.5, 34.1_

  - [x] 13.3 Implement `StoryTaskController`
    - `GET /api/v1/stories/{storyId}/tasks` — Bearer — List TaskDetailResponse for story
    - _Requirements: 7.4, 34.1_

  - [x] 13.4 Implement `TaskController`
    - `POST /api/v1/tasks` — Bearer (OrgAdmin, DeptLead, Member) — CreateTaskRequest → 201 TaskDetailResponse
    - `GET /api/v1/tasks/{id}` — Bearer — TaskDetailResponse
    - `PUT /api/v1/tasks/{id}` — Bearer (OrgAdmin, DeptLead, Member) — UpdateTaskRequest → TaskDetailResponse
    - `DELETE /api/v1/tasks/{id}` — Bearer (OrgAdmin, DeptLead) — Soft delete
    - `PATCH /api/v1/tasks/{id}/status` — Bearer (Member+) — TaskStatusRequest → TaskDetailResponse
    - `PATCH /api/v1/tasks/{id}/assign` — Bearer (DeptLead+) — TaskAssignRequest → TaskDetailResponse
    - `PATCH /api/v1/tasks/{id}/self-assign` — Bearer (Member+) — 200 TaskDetailResponse
    - `PATCH /api/v1/tasks/{id}/unassign` — Bearer (DeptLead+) — 200
    - `PATCH /api/v1/tasks/{id}/log-hours` — Bearer (Member+) — LogHoursRequest → 200
    - `GET /api/v1/tasks/{id}/activity` — Bearer — List ActivityLogResponse
    - `GET /api/v1/tasks/{id}/comments` — Bearer — List CommentResponse
    - `GET /api/v1/tasks/suggest-assignee` — Bearer — SuggestAssigneeResponse
    - _Requirements: 7.1, 7.3, 7.5, 7.6, 8.2, 8.5, 8.6, 9.2, 10.1, 34.1_

  - [x] 13.5 Implement `SprintController`
    - `POST /api/v1/projects/{projectId}/sprints` — Bearer (OrgAdmin, DeptLead) — CreateSprintRequest → 201 SprintDetailResponse
    - `GET /api/v1/sprints` — Bearer — Paginated SprintListResponse (filterable by status, projectId)
    - `GET /api/v1/sprints/{id}` — Bearer — SprintDetailResponse (project info, stories, metrics, burndown)
    - `PUT /api/v1/sprints/{id}` — Bearer (OrgAdmin, DeptLead) — UpdateSprintRequest → SprintDetailResponse
    - `PATCH /api/v1/sprints/{id}/start` — Bearer (OrgAdmin, DeptLead) — 200 SprintDetailResponse
    - `PATCH /api/v1/sprints/{id}/complete` — Bearer (OrgAdmin, DeptLead) — 200 SprintDetailResponse
    - `PATCH /api/v1/sprints/{id}/cancel` — Bearer (OrgAdmin, DeptLead) — 200 SprintDetailResponse
    - `POST /api/v1/sprints/{sprintId}/stories` — Bearer (OrgAdmin, DeptLead) — AddStoryToSprintRequest → 200
    - `DELETE /api/v1/sprints/{sprintId}/stories/{storyId}` — Bearer (OrgAdmin, DeptLead) — 200
    - `GET /api/v1/sprints/{id}/metrics` — Bearer — SprintMetricsResponse
    - `GET /api/v1/sprints/velocity` — Bearer — List VelocityResponse
    - `GET /api/v1/sprints/active` — Bearer — SprintDetailResponse (optional ?projectId)
    - _Requirements: 12.1, 12.3, 12.4, 12.5, 13.1, 13.2, 13.3, 13.4, 14.1, 14.4, 15.1, 16.1, 34.1_

  - [x] 13.6 Implement `BoardController`
    - `GET /api/v1/boards/kanban` — Bearer — KanbanBoardResponse (optional ?projectId, ?sprintId, ?departmentId, ?assigneeId, ?priority, ?labels)
    - `GET /api/v1/boards/sprint` — Bearer — SprintBoardResponse (optional ?projectId)
    - `GET /api/v1/boards/backlog` — Bearer — BacklogResponse (optional ?projectId)
    - `GET /api/v1/boards/department` — Bearer — DepartmentBoardResponse (optional ?projectId, ?sprintId)
    - _Requirements: 17.1, 18.1, 19.1, 20.1, 34.1_

  - [x] 13.7 Implement `CommentController`
    - `POST /api/v1/comments` — Bearer (Member+) — CreateCommentRequest → 201 CommentResponse
    - `PUT /api/v1/comments/{id}` — Bearer (Author only) — {content} → CommentResponse
    - `DELETE /api/v1/comments/{id}` — Bearer (Author, OrgAdmin) — 200
    - _Requirements: 21.1, 21.4, 21.6, 34.1_

  - [x] 13.8 Implement `LabelController`
    - `POST /api/v1/labels` — Bearer (DeptLead+) — CreateLabelRequest → 201 LabelResponse
    - `GET /api/v1/labels` — Bearer — List LabelResponse
    - `PUT /api/v1/labels/{id}` — Bearer (DeptLead+) — UpdateLabelRequest → LabelResponse
    - `DELETE /api/v1/labels/{id}` — Bearer (OrgAdmin) — 200
    - _Requirements: 23.1, 23.6, 23.7, 23.8, 34.1_

  - [x] 13.9 Implement `SearchController`
    - `GET /api/v1/search` — Bearer — SearchRequest (query params) → SearchResponse
    - _Requirements: 25.1, 34.1_

  - [x] 13.10 Implement `ReportController`
    - `GET /api/v1/reports/velocity` — Bearer — VelocityChartResponse (optional ?count)
    - `GET /api/v1/reports/department-workload` — Bearer — DepartmentWorkloadResponse (optional ?sprintId)
    - `GET /api/v1/reports/capacity` — Bearer — CapacityUtilizationResponse (optional ?departmentId)
    - `GET /api/v1/reports/cycle-time` — Bearer — CycleTimeResponse (optional ?dateFrom, ?dateTo)
    - `GET /api/v1/reports/task-completion` — Bearer — TaskCompletionResponse (optional ?sprintId, ?dateFrom, ?dateTo)
    - _Requirements: 27.1, 28.1, 29.1, 30.1, 31.1, 34.1_

  - [x] 13.11 Implement `WorkflowController`
    - `GET /api/v1/workflows` — Bearer — WorkflowDefinitionResponse
    - `PUT /api/v1/workflows/organization` — OrgAdmin — WorkflowOverrideRequest → 200
    - `PUT /api/v1/workflows/department/{departmentId}` — OrgAdmin, DeptLead — WorkflowOverrideRequest → 200
    - _Requirements: 32.1, 32.2, 32.3, 34.1_

  - [x] 13.12 Implement `SavedFilterController`
    - `POST /api/v1/saved-filters` — Bearer — CreateSavedFilterRequest → 201 SavedFilterResponse
    - `GET /api/v1/saved-filters` — Bearer — List SavedFilterResponse
    - `DELETE /api/v1/saved-filters/{id}` — Bearer — 200
    - _Requirements: 26.1, 26.2, 26.3, 34.1_

- [x] 14. Api layer — Program.cs, extensions, Dockerfile
  - [x] 14.1 Create `Program.cs` with full DI registration and middleware pipeline
    - Load `.env` via DotNetEnv, build `AppSettings`
    - Register Infrastructure services via `DependencyInjection` extension
    - Register FluentValidation validators (auto-discovery), suppress ModelStateInvalidFilter
    - Register JWT Bearer authentication
    - Register CORS with `AllowedOrigins`
    - Register health checks (PostgreSQL + Redis)
    - Register Swagger (Development mode only)
    - Apply `DatabaseMigrationHelper` on startup
    - Build middleware pipeline in correct order via `MiddlewarePipelineExtensions`
    - Map controllers, health check endpoints (`/health`, `/ready`)
    - _Requirements: 41.3, 45.1, 46.1, 46.2, 51.1, 52.1, 53.1, 54.1, 57.1_

  - [x] 14.2 Create `ControllerServiceExtensions` for controller-specific DI
    - Register controllers with `ApiResponse<T>` envelope conventions
    - _Requirements: 40.1_

  - [x] 14.3 Create `SwaggerServiceExtensions`
    - Configure Swagger with JWT Bearer auth support, API info
    - Development mode only
    - _Requirements: 54.1, 54.2_

  - [x] 14.4 Create `HealthCheckExtensions`
    - Register PostgreSQL and Redis health checks
    - Map `/health` (liveness) and `/ready` (readiness) endpoints
    - _Requirements: 46.1, 46.2_

  - [x] 14.5 Create `Dockerfile` and `.env.example`
    - Multi-stage Dockerfile for WorkService.Api
    - `.env.example` documenting all environment variables
    - _Requirements: 52.1_

  - [x] 14.6 Configure structured logging conventions
    - Ensure `GlobalExceptionHandlerMiddleware` logs DomainExceptions with: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName` ("WorkService"), `RequestPath`
    - Ensure unhandled exception logs include: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`
    - Ensure downstream call failure logs include: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`
    - _Requirements: 49.1, 49.2, 49.3_

- [x] 15. Checkpoint — Full build verification
  - Ensure all projects compile, all tests pass, ask the user if questions arise.

- [x] 16. Testing
  - [x]* 16.1 Write property tests for story key format (Property 1)
    - **Property 1: Story key matches project key prefix**
    - For any project with a valid ProjectKey and any story created within that project, the generated StoryKey matches `{ProjectKey}-{N}` where N is a positive integer
    - **Validates: Requirements 1.1, 3.1**

  - [x]* 16.2 Write property tests for per-project sequence monotonicity (Property 2)
    - **Property 2: Per-project sequence monotonicity**
    - For any project and any sequence of N story creations, the resulting sequence numbers are exactly 1, 2, 3, ..., N with no gaps and no duplicates
    - **Validates: Requirements 1.3**

  - [x]* 16.3 Write property tests for ProjectKey format validation (Property 3)
    - **Property 3: ProjectKey format validation**
    - For any string not matching `^[A-Z0-9]{2,10}$`, project creation is rejected with PROJECT_KEY_INVALID_FORMAT (4045)
    - **Validates: Requirements 2.2**

  - [x]* 16.4 Write property tests for project uniqueness constraints (Property 4)
    - **Property 4: Project uniqueness constraints**
    - Duplicate ProjectKey (globally) → PROJECT_KEY_DUPLICATE (4043). Duplicate ProjectName (within org) → PROJECT_NAME_DUPLICATE (4042)
    - **Validates: Requirements 2.3, 2.4**

  - [x]* 16.5 Write property tests for ProjectKey immutability (Property 5)
    - **Property 5: ProjectKey immutability when stories exist**
    - Project with stories → ProjectKey update rejected (4044). Project without stories → update succeeds
    - **Validates: Requirements 2.7**

  - [x] 16.6 Write property tests for story field validation (Property 6)
    - **Property 6: Story field validation — Fibonacci points and priority**
    - Non-Fibonacci integers rejected for story points. Non-valid strings rejected for priority
    - **Validates: Requirements 3.7, 3.8**

  - [x]* 16.7 Write property tests for initial entity status invariant (Property 7)
    - **Property 7: Initial entity status invariant**
    - New story → Backlog. New task → ToDo. New sprint → Planning
    - **Validates: Requirements 4.1, 9.1, 12.1**

  - [x]* 16.8 Write property tests for workflow state machine (Property 8)
    - **Property 8: Workflow state machine enforces valid transitions**
    - For any (from, to) pair, transition succeeds iff it's in the valid transitions map for both stories and tasks
    - **Validates: Requirements 4.2, 4.3, 9.2, 9.3**

  - [x]* 16.9 Write property tests for story transition preconditions (Property 9)
    - **Property 9: Story transition preconditions**
    - Ready without description → rejected. Ready without points → rejected. InProgress without assignee → rejected. InReview without tasks → rejected
    - **Validates: Requirements 4.4, 4.5, 4.6, 4.7**

  - [x]* 16.10 Write property tests for CompletedDate on Done (Property 10)
    - **Property 10: CompletedDate set on Done transition**
    - Any story or task transitioning to Done has CompletedDate set to non-null UTC value
    - **Validates: Requirements 4.8, 9.4**

  - [x]* 16.11 Write property tests for activity log on status change (Property 11)
    - **Property 11: Activity log creation on status change**
    - Any story or task status change creates an ActivityLog entry with Action=StatusChanged, correct OldValue/NewValue
    - **Validates: Requirements 4.9, 9.5, 24.1**

  - [x]* 16.12 Write property tests for story link bidirectionality (Property 12)
    - **Property 12: Story link bidirectionality round-trip**
    - Creating blocks A→B also creates is_blocked_by B→A. Deleting either removes both
    - **Validates: Requirements 6.1, 6.2, 6.4**

  - [x]* 16.13 Write property tests for task department auto-mapping (Property 13)
    - **Property 13: Task department auto-mapping**
    - For any valid task type, the auto-mapped department code matches TaskTypeDepartmentMap
    - **Validates: Requirements 7.1, 8.1**

  - [x]* 16.14 Write property tests for task type validation (Property 14)
    - **Property 14: Task type validation**
    - Any string not in valid task types is rejected with INVALID_TASK_TYPE (4025)
    - **Validates: Requirements 7.2**

  - [x]* 16.15 Write property tests for hour logging accumulation (Property 17)
    - **Property 17: Hour logging accumulation**
    - For any sequence of positive hour values, ActualHours equals the sum. Non-positive values rejected
    - **Validates: Requirements 10.1, 10.2**

  - [x]* 16.16 Write property tests for completion percentage (Property 18)
    - **Property 18: Completion percentage calculation**
    - For N total tasks and M completed, percentage = M/N*100 (or 0 if N=0)
    - **Validates: Requirements 11.1**

  - [x]* 16.17 Write property tests for sprint date validation (Property 19)
    - **Property 19: Sprint end-after-start validation**
    - EndDate <= StartDate → rejected with SPRINT_END_BEFORE_START (4033)
    - **Validates: Requirements 12.2**

  - [x]* 16.18 Write property tests for one active sprint per project (Property 20)
    - **Property 20: One active sprint per project**
    - Project with active sprint → starting another rejected with ONLY_ONE_ACTIVE_SPRINT (4016)
    - **Validates: Requirements 13.2**

  - [x]* 16.19 Write property tests for max labels per story (Property 23)
    - **Property 23: Max labels per story**
    - Story with 10 labels → applying 11th rejected with MAX_LABELS_PER_STORY (4040)
    - **Validates: Requirements 23.4**

  - [x]* 16.20 Write property tests for pagination correctness (Property 25)
    - **Property 25: Pagination correctness**
    - TotalPages = ceil(TotalCount / PageSize), Data length <= PageSize
    - **Validates: Requirements 47.1, 47.2, 47.3**

  - [x]* 16.21 Write property tests for soft delete exclusion (Property 26)
    - **Property 26: Soft delete exclusion**
    - Soft-deleted entities (FlgStatus=D) are excluded from standard queries
    - **Validates: Requirements 48.1, 48.2**

  - [x]* 16.22 Write unit tests for WorkflowStateMachine helper
    - Test all valid story transitions (Backlog→Ready, Ready→InProgress, etc.)
    - Test all valid task transitions (ToDo→InProgress, InProgress→InReview, etc.)
    - Test invalid transitions return false
    - **Validates: Requirements 4.2, 4.3, 9.2, 9.3**

  - [x]* 16.23 Write unit tests for TaskTypeDepartmentMap helper
    - Test all 6 task type → department code mappings
    - Test invalid task type throws InvalidTaskTypeException
    - **Validates: Requirements 8.1**

  - [x]* 16.24 Write unit tests for FluentValidation validators
    - Test each validator with valid and invalid inputs
    - Verify ProjectKey regex, Fibonacci story points, priority values, task types
    - Verify sprint EndDate > StartDate, search query min length, hours > 0
    - **Validates: Requirements 41.1, 41.2**

  - [x]* 16.25 Write unit tests for middleware pipeline
    - Verify middleware registration order matches Requirement 57.1 specification
    - Test GlobalExceptionHandlerMiddleware returns correct ApiResponse for DomainException and unhandled exceptions
    - Test TokenBlacklistMiddleware rejects blacklisted tokens
    - Test CorrelationIdMiddleware generates/propagates correlation IDs
    - **Validates: Requirements 39, 43, 57**

  - [x]* 16.26 Write unit tests for error code resolver static mapping
    - **Property: Every known error code in ErrorCodes maps to a non-empty response code, and the mapping is deterministic**
    - **Validates: Requirements 56.1, 56.2**

- [x] 17. Final checkpoint — Full integration verification
  - Ensure all projects compile, all tests pass, ask the user if questions arise.

## Notes

- All WorkService projects live under `src/backend/WorkService/` in the monorepo
- Tests are co-located at `src/backend/WorkService/WorkService.Tests/`
- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- The implementation language is C# (.NET 8) as specified in the design document
- Checkpoints ensure incremental validation at layer boundaries
- Property tests validate universal correctness properties from the design document (26 properties)
- Unit tests validate specific examples, edge cases, and integration points
- WorkService does NOT include FirstTimeUserMiddleware — that is enforced by SecurityService
- StorySequence entity is NOT organization-scoped — accessed directly by ProjectId PK
- All other entities implement IOrganizationEntity and are filtered by OrganizationId via global query filters
- The Project entity is included throughout: scaffolding, domain entities, repository, service, controller, and testing
