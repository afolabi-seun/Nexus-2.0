# Implementation Plan: WorkService ServiceResult Migration

## Overview

Migrate all 13 WorkService service interfaces, implementations, and 18 controllers from exception-driven control flow to the `ServiceResult<T>` return-value pattern. The migration follows a 5-group order: Foundation → Core CRUD → Supporting Services → Time Tracking → Analytics/Risk/Templates/Export. Each service migration is atomic (interface + implementation + controller + test updates) and the system must compile with all tests passing after each sub-task.

## Tasks

- [ ] 1. Foundation — ServiceResult class and ToActionResult extension
  - [ ] 1.1 Create `ServiceResult<T>` class at `WorkService.Domain/Results/ServiceResult.cs`
    - Copy the exact structure from `BillingService.Domain.Results.ServiceResult<T>`
    - Expose properties: `IsSuccess`, `Data`, `Message`, `StatusCode`, `ErrorValue`, `ErrorCode`
    - Implement static factory methods: `Ok`, `Created`, `NoContent`, `Fail`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_

  - [ ]* 1.2 Write property tests for ServiceResult factory methods
    - Create `WorkService.Tests/Property/ServiceResultPropertyTests.cs`
    - **Property 1: ServiceResult factory methods produce correct state**
    - Use FsCheck.Xunit to generate random data values, messages, error codes, and status codes
    - Verify `Ok` → `IsSuccess=true, StatusCode=200`, `Created` → `IsSuccess=true, StatusCode=201`, `NoContent` → `IsSuccess=true, StatusCode=204, Data=default`, `Fail` → `IsSuccess=false` with provided error fields
    - **Validates: Requirements 1.3, 1.4, 1.5, 1.6, 1.7**

  - [ ] 1.3 Create `ToActionResult` extension at `WorkService.Api/Extensions/ServiceResultExtensions.cs`
    - Copy the exact structure from `BillingService.Api.Extensions.ServiceResultExtensions`
    - Handle success → `ApiResponse<T>.Ok(...)`, failure → `ApiResponse<object>.Fail(...)`, null → 500 `INTERNAL_ERROR`
    - Inject `CorrelationId` from `HttpContext.Items["CorrelationId"]`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

  - [ ]* 1.4 Write property tests for ToActionResult conversion
    - Create `WorkService.Tests/Property/ServiceResultExtensionsPropertyTests.cs`
    - **Property 2: ToActionResult conversion preserves ServiceResult semantics**
    - Use FsCheck.Xunit to generate random `ServiceResult<T>` instances (both success and failure)
    - Verify the `ObjectResult.StatusCode` matches `ServiceResult.StatusCode`, and `ApiResponse` fields match
    - **Validates: Requirements 2.2, 2.3, 2.5**

- [ ] 2. Checkpoint — Foundation compiles and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 3. Core CRUD — Project Service migration
  - [ ] 3.1 Migrate `IProjectService` interface and `ProjectService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` with typed DTOs (`ProjectDetailResponse`, `PaginatedResponse<ProjectListResponse>`)
    - Change `object request` parameters to typed DTOs (`CreateProjectRequest`, `UpdateProjectRequest`)
    - Change `Task UpdateStatusAsync` to `Task<ServiceResult<object>>`
    - Replace all `throw new ProjectXxxException(...)` with `ServiceResult.Fail(...)` using error codes from the design's conversion table
    - Return `ServiceResult.Created(...)` for CreateAsync, `ServiceResult.Ok(...)` for Get/List/Update, `ServiceResult.NoContent(...)` for UpdateStatusAsync
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

  - [ ] 3.2 Migrate `ProjectController` to one-liner pattern
    - Replace all `ApiResponse<object>.Ok(result, msg).ToActionResult(HttpContext, statusCode)` with `(await _service.Method(...)).ToActionResult(HttpContext)`
    - Remove explicit status code parameters (201 is now set by `ServiceResult.Created`)
    - Remove `null!` for void operations — use `ServiceResult.NoContent` from service
    - Note: `GetCostSummary`, `GetUtilization`, `GetCostSnapshots`, `ExportStories`, `ExportTimeEntries` endpoints depend on services migrated in Groups 4/5 — leave those actions unchanged for now
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

  - [ ] 3.3 Update `ProjectServiceTests.cs` assertions
    - Change exception assertions (`Assert.ThrowsAsync<XxxException>`) to ServiceResult property assertions (`result.IsSuccess`, `result.StatusCode`, `result.ErrorCode`)
    - Change success assertions to verify `result.IsSuccess == true`, `result.StatusCode`, and `result.Data`
    - _Requirements: 16.2, 16.3, 16.4_

  - [ ]* 3.4 Write property test for invalid project key format rejection
    - Create `WorkService.Tests/Property/ProjectServicePropertyTests.cs`
    - **Property 3: Invalid project key format rejection**
    - Use FsCheck.Xunit to generate random strings not matching `^[A-Z0-9]{2,10}$`
    - Verify `CreateAsync` returns `IsSuccess=false`, `ErrorCode="PROJECT_KEY_INVALID_FORMAT"`, `StatusCode=400`
    - **Validates: Requirements 3.4**

- [ ] 4. Core CRUD — Story Service migration
  - [ ] 4.1 Migrate `IStoryService` interface and `StoryService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` with typed DTOs
    - Change all `Task` (void) return types (`DeleteAsync`, `CreateLinkAsync`, `DeleteLinkAsync`, `ApplyLabelAsync`, `RemoveLabelAsync`, `UnassignAsync`) to `Task<ServiceResult<object>>` returning `NoContent`
    - Replace `throw new StoryNotFoundException(...)` with `ServiceResult.Fail(4001, "STORY_NOT_FOUND", msg, 404)`
    - Replace `throw new InvalidStoryTransitionException(...)` with `ServiceResult.Fail(4004, "INVALID_STORY_TRANSITION", msg, 400)`
    - Replace `throw new StoryRequiresAssigneeException(...)` with `ServiceResult.Fail(4013, "STORY_REQUIRES_ASSIGNEE", msg, 400)`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [ ] 4.2 Migrate `StoryController` and `StoryTaskController` to one-liner pattern
    - Replace all manual `ApiResponse` construction with `.ToActionResult(HttpContext)` calls
    - Remove try/catch blocks and manual status code parameters
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

  - [ ] 4.3 Update `StoryServiceTests.cs` assertions
    - Change exception assertions to ServiceResult property assertions
    - Change success assertions to verify ServiceResult properties
    - _Requirements: 16.2, 16.3, 16.4_

  - [ ]* 4.4 Write property tests for Story Service
    - Create `WorkService.Tests/Property/StoryServicePropertyTests.cs`
    - **Property 4: Invalid story status transition rejection**
    - **Property 5: Bulk operation success/failure counting**
    - Use FsCheck.Xunit to generate random invalid status transitions and random lists of story IDs
    - **Validates: Requirements 4.3, 4.5**

- [ ] 5. Core CRUD — Task Service migration
  - [ ] 5.1 Migrate `ITaskService` interface and `TaskService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` with typed DTOs
    - Change `Task DeleteAsync`, `Task UnassignAsync`, `Task LogHoursAsync` to `Task<ServiceResult<object>>` returning `NoContent`
    - Replace `throw new TaskNotFoundException(...)` with `ServiceResult.Fail(4002, "TASK_NOT_FOUND", msg, 404)`
    - Replace `throw new InvalidTaskTransitionException(...)` with `ServiceResult.Fail(4005, "INVALID_TASK_TRANSITION", msg, 400)`
    - Replace `throw new AssigneeNotInDepartmentException(...)` with `ServiceResult.Fail(4018, "ASSIGNEE_NOT_IN_DEPARTMENT", msg, 400)`
    - Replace `throw new AssigneeAtCapacityException(...)` with `ServiceResult.Fail(4019, "ASSIGNEE_AT_CAPACITY", msg, 400)`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [ ] 5.2 Migrate `TaskController` to one-liner pattern
    - Replace all manual `ApiResponse` construction with `.ToActionResult(HttpContext)` calls
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

  - [ ] 5.3 Update `TaskServiceTests.cs` assertions
    - Change exception assertions to ServiceResult property assertions
    - _Requirements: 16.2, 16.3, 16.4_

  - [ ]* 5.4 Write property test for invalid task status transition rejection
    - Create `WorkService.Tests/Property/TaskServicePropertyTests.cs`
    - **Property 6: Invalid task status transition rejection**
    - Use FsCheck.Xunit to generate random invalid task status transition pairs
    - **Validates: Requirements 5.3**

- [ ] 6. Core CRUD — Sprint Service migration
  - [ ] 6.1 Migrate `ISprintService` interface and `SprintService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` with typed DTOs
    - Change `Task AddStoryAsync`, `Task RemoveStoryAsync` to `Task<ServiceResult<object>>` returning `NoContent`
    - Replace `throw new SprintNotFoundException(...)` with `ServiceResult.Fail(4003, "SPRINT_NOT_FOUND", msg, 404)`
    - Replace `throw new SprintOverlapException(...)` with `ServiceResult.Fail(4009, "SPRINT_OVERLAP", msg, 409)`
    - Replace `throw new SprintAlreadyActiveException(...)` with `ServiceResult.Fail(4021, "SPRINT_ALREADY_ACTIVE", msg, 400)`
    - Replace `throw new SprintAlreadyCompletedException(...)` with `ServiceResult.Fail(4022, "SPRINT_ALREADY_COMPLETED", msg, 400)`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [ ] 6.2 Migrate `SprintController` to one-liner pattern
    - Replace all manual `ApiResponse` construction with `.ToActionResult(HttpContext)` calls
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

  - [ ] 6.3 Update `SprintServiceTests.cs` assertions
    - Change exception assertions to ServiceResult property assertions
    - _Requirements: 16.2, 16.3, 16.4_

  - [ ]* 6.4 Write property test for sprint date overlap detection
    - Create `WorkService.Tests/Property/SprintServicePropertyTests.cs`
    - **Property 7: Sprint date overlap detection**
    - Use FsCheck.Xunit to generate random overlapping date range pairs within the same project
    - **Validates: Requirements 6.3**

- [ ] 7. Checkpoint — Core CRUD services compile and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Supporting Services — Comment Service migration
  - [ ] 8.1 Migrate `ICommentService` interface and `CommentService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` with typed DTOs
    - Change `Task` (void) return types to `Task<ServiceResult<object>>` returning `NoContent`
    - Replace `throw new CommentNotFoundException(...)` with `ServiceResult.Fail(4012, "COMMENT_NOT_FOUND", msg, 404)`
    - Replace `throw new CommentNotAuthorException(...)` with `ServiceResult.Fail(4017, "COMMENT_NOT_AUTHOR", msg, 403)`
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [ ] 8.2 Migrate `CommentController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 9. Supporting Services — Label Service migration
  - [ ] 9.1 Migrate `ILabelService` interface and `LabelService` implementation
    - Change all return types to `Task<ServiceResult<T>>`
    - Replace `throw new LabelNameDuplicateException(...)` with `ServiceResult.Fail(4011, "LABEL_NAME_DUPLICATE", msg, 409)`
    - Replace `throw new LabelNotFoundException(...)` with `ServiceResult.Fail(4010, "LABEL_NOT_FOUND", msg, 404)`
    - _Requirements: 8.1, 8.2, 8.3, 8.4_

  - [ ] 9.2 Migrate `LabelController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 10. Supporting Services — Search, Board, Report Services migration
  - [ ] 10.1 Migrate `ISearchService` interface and `SearchService` implementation
    - Change `Task<object>` to `Task<ServiceResult<T>>` for `SearchAsync`
    - Replace `throw new SearchQueryTooShortException(...)` with `ServiceResult.Fail(4028, "SEARCH_QUERY_TOO_SHORT", msg, 400)`
    - _Requirements: 9.1, 9.2_

  - [ ] 10.2 Migrate `SearchController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

  - [ ]* 10.3 Write property test for short search query rejection
    - Create `WorkService.Tests/Property/SearchServicePropertyTests.cs`
    - **Property 8: Short search query rejection**
    - Use FsCheck.Xunit to generate random strings shorter than the minimum required length
    - **Validates: Requirements 9.2**

  - [ ] 10.4 Migrate `IBoardService` interface and `BoardService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>`
    - Return `ServiceResult.Ok(data, message)` for all board methods
    - _Requirements: 9.3, 9.5_

  - [ ] 10.5 Migrate `BoardController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

  - [ ] 10.6 Migrate `IReportService` interface and `ReportService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>`
    - Return `ServiceResult.Ok(data, message)` for all report methods
    - _Requirements: 9.4, 9.5_

  - [ ] 10.7 Migrate `ReportController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 11. Supporting Services — Workflow Service migration
  - [ ] 11.1 Migrate `IWorkflowService` interface and `WorkflowService` implementation
    - Change `Task<object>` to `Task<ServiceResult<T>>` for `GetWorkflowsAsync`
    - Change `Task` (void) to `Task<ServiceResult<object>>` for `SaveOrganizationOverrideAsync` and `SaveDepartmentOverrideAsync`, returning `NoContent`
    - _Requirements: 10.1, 10.2, 10.3_

  - [ ] 11.2 Migrate `WorkflowController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 12. Checkpoint — Supporting services compile and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Time Tracking — TimeEntry Service migration
  - [ ] 13.1 Migrate `ITimeEntryService` interface and `TimeEntryService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` and `Task` (void) to `Task<ServiceResult<object>>`
    - Replace `throw new TimeEntryNotFoundException(...)` with `ServiceResult.Fail(4052, "TIME_ENTRY_NOT_FOUND", msg, 404)`
    - Replace `throw new DailyHoursExceededException(...)` with `ServiceResult.Fail(4056, "DAILY_HOURS_EXCEEDED", msg, 400)`
    - _Requirements: 11.1, 11.2, 11.3_

  - [ ] 13.2 Migrate `TimeEntryController` to one-liner pattern
    - Also update the `GetCostSummary` and `GetUtilization` endpoints in `ProjectController` that delegate to `ITimeEntryService`
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

  - [ ]* 13.3 Write property test for daily hours exceeded rejection
    - Create `WorkService.Tests/Property/TimeEntryServicePropertyTests.cs`
    - **Property 9: Daily hours exceeded rejection**
    - Use FsCheck.Xunit to generate random hours values that exceed the daily maximum
    - **Validates: Requirements 11.3**

- [ ] 14. Time Tracking — TimePolicy Service migration
  - [ ] 14.1 Migrate `ITimePolicyService` interface and `TimePolicyService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>`
    - _Requirements: 11.4_

  - [ ] 14.2 Migrate `TimePolicyController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 15. Time Tracking — CostRate Service migration
  - [ ] 15.1 Migrate `ICostRateService` interface and `CostRateService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` and `Task` (void) to `Task<ServiceResult<object>>`
    - Replace `throw new CostRateDuplicateException(...)` with `ServiceResult.Fail(4053, "COST_RATE_DUPLICATE", msg, 409)`
    - _Requirements: 11.5, 11.6_

  - [ ] 15.2 Migrate `CostRateController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 16. Time Tracking — TimerSession Service migration
  - [ ] 16.1 Migrate `ITimerSessionService` interface and `TimerSessionService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>`
    - Replace `throw new TimerAlreadyActiveException(...)` with `ServiceResult.Fail(4050, "TIMER_ALREADY_ACTIVE", msg, 400)`
    - Replace `throw new NoActiveTimerException(...)` with `ServiceResult.Fail(4051, "NO_ACTIVE_TIMER", msg, 404)`
    - Note: TimerSession endpoints are served by `TimeEntryController` — update those actions there
    - _Requirements: 11.7, 11.8, 11.9_

  - [ ] 16.2 Update TimerSession-related actions in `TimeEntryController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 17. Checkpoint — Time tracking services compile and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 18. Analytics and Risk — Analytics Service migration
  - [ ] 18.1 Migrate `IAnalyticsService` interface and `AnalyticsService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` and `Task` (void) to `Task<ServiceResult<object>>`
    - Replace `throw new InvalidAnalyticsParameterException(...)` with `ServiceResult.Fail(4060, "INVALID_ANALYTICS_PARAMETER", msg, 400)`
    - Replace `throw new SnapshotGenerationFailedException(...)` with `ServiceResult.Fail(4065, "SNAPSHOT_GENERATION_FAILED", msg, 500)`
    - _Requirements: 12.1, 12.2, 12.3_

  - [ ] 18.2 Migrate `IAnalyticsSnapshotService` interface and `AnalyticsSnapshotService` implementation
    - Change `Task<object>` to `Task<ServiceResult<T>>` for `GetSnapshotStatusAsync`
    - Change `Task` (void) to `Task<ServiceResult<object>>` for `TriggerSprintCloseSnapshotsAsync` and `GeneratePeriodicSnapshotsAsync`
    - _Requirements: 12.4_

  - [ ] 18.3 Migrate `AnalyticsController` to one-liner pattern
    - Covers both AnalyticsService and AnalyticsSnapshotService endpoints
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 19. Analytics and Risk — CostSnapshot and RiskRegister migration
  - [ ] 19.1 Migrate `ICostSnapshotService` interface and `CostSnapshotService` implementation
    - Change `Task<object>` to `Task<ServiceResult<T>>` for `ListByProjectAsync`
    - Update the `GetCostSnapshots` endpoint in `ProjectController` to use one-liner pattern
    - _Requirements: 12.5_

  - [ ] 19.2 Migrate `IRiskRegisterService` interface and `RiskRegisterService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` and `Task` (void) to `Task<ServiceResult<object>>`
    - Replace `throw new RiskNotFoundException(...)` with `ServiceResult.Fail(4064, "RISK_NOT_FOUND", msg, 404)`
    - _Requirements: 12.6, 12.7_

  - [ ] 19.3 Migrate `RiskRegisterController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 20. Templates, Export, SavedFilter — StoryTemplate Service migration
  - [ ] 20.1 Migrate `IStoryTemplateService` interface and `StoryTemplateService` implementation
    - Change all `Task<object>` return types to `Task<ServiceResult<T>>` and `Task` (void) to `Task<ServiceResult<object>>`
    - Return `ServiceResult.Created(data, "Story template created.")` for CreateAsync
    - Return `ServiceResult.NoContent("Story template deleted.")` for DeleteAsync
    - _Requirements: 13.1, 13.2, 13.3_

  - [ ] 20.2 Migrate `StoryTemplateController` to one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

- [ ] 21. Templates, Export, SavedFilter — Export Service migration
  - [ ] 21.1 Migrate `IExportService` interface and `ExportService` implementation
    - Change `Task<byte[]>` return types to `Task<ServiceResult<byte[]>>` for both export methods
    - Return `ServiceResult.Ok(csvBytes, "Export generated.")`
    - _Requirements: 13.4, 13.5_

  - [ ] 21.2 Update export endpoints in `ProjectController` to one-liner pattern
    - The `ExportStories` and `ExportTimeEntries` actions need special handling: extract `result.Data` for the `File()` return since CSV exports return file content, not JSON
    - _Requirements: 14.1, 14.2_

- [ ] 22. Templates, Export, SavedFilter — SavedFilter Service creation and migration
  - [ ] 22.1 Create `ISavedFilterService` interface and `SavedFilterService` implementation
    - Create new `WorkService.Domain/Interfaces/Services/SavedFilters/ISavedFilterService.cs`
    - Create new `WorkService.Infrastructure/Services/SavedFilters/SavedFilterService.cs`
    - Move business logic from `SavedFilterController` into the new service
    - Methods: `CreateAsync`, `ListAsync`, `DeleteAsync` — all returning `Task<ServiceResult<T>>`
    - Return `ServiceResult.Created(...)` for create, `ServiceResult.Ok(...)` for list, `ServiceResult.NoContent(...)` for delete
    - Handle filter-not-found as `ServiceResult.Fail` with `FILTER_NOT_FOUND` error code
    - Register `ISavedFilterService` / `SavedFilterService` in DI container
    - _Requirements: 14.6_

  - [ ] 22.2 Migrate `SavedFilterController` to one-liner pattern
    - Replace direct repository calls with `ISavedFilterService` delegation
    - Remove `WorkDbContext` dependency from controller
    - Use `.ToActionResult(HttpContext)` one-liner pattern
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.6_

- [ ] 23. Checkpoint — All Group 5 services compile and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 24. Final verification and cleanup
  - [ ] 24.1 Verify all 18 controllers use the one-liner `.ToActionResult(HttpContext)` pattern
    - Confirm no controllers contain manual `ApiResponse` construction, try/catch blocks, or explicit status code parameters
    - Confirm `IStoryIdGenerator` and `IActivityLogService.LogAsync` remain unchanged
    - Confirm all `DomainException` subclasses are preserved in `WorkService.Domain.Exceptions`
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 15.1, 15.2, 15.3, 15.4, 16.5, 16.6_

- [ ] 25. Final checkpoint — Full solution compiles and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional property-based tests and can be skipped for faster MVP
- Each service migration is atomic: interface + implementation + controller + test updates must be completed together
- The system must compile and all tests pass after every sub-task
- DomainExceptions thrown from repositories/deep code are preserved — only service-level exceptions are converted to `ServiceResult.Fail()`
- `IStoryIdGenerator` and `IActivityLogService.LogAsync` are excluded from migration per design
- Property tests use FsCheck.Xunit (already in `WorkService.Tests.csproj`)
- The `SavedFilterController` special case (task 22) introduces a new service layer before migrating to the one-liner pattern
