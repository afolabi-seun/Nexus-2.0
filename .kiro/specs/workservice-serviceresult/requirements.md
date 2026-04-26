# Requirements Document

## Introduction

The WorkService is the last of five backend services that has not been migrated to the ServiceResult pattern. The four already-migrated services (SecurityService, ProfileService, BillingService, UtilityService) return `ServiceResult<T>` from service methods, enabling thin one-liner controllers and a unified error-handling flow. This migration brings WorkService into alignment by replacing untyped `Task<object>` returns and exception-driven control flow with the same `ServiceResult<T>` pattern used across the platform.

## Glossary

- **ServiceResult**: A generic result wrapper class (`ServiceResult<T>`) that encapsulates success/failure state, typed data, HTTP status code, error code, and error message. Returned by service methods instead of throwing exceptions for expected business failures.
- **ToActionResult**: An extension method on `ServiceResult<T>` that converts the result into an `IActionResult` wrapped in the `ApiResponse<T>` envelope, including correlation ID injection.
- **ApiResponse**: The standard JSON envelope (`ApiResponse<T>`) used by all API responses, containing `success`, `data`, `errorCode`, `errorValue`, `message`, `correlationId`, and `responseCode` fields.
- **DomainException**: The base exception class for expected error conditions that cannot be expressed as `ServiceResult` (e.g., thrown from deep in the call stack such as repositories or validators). Caught by `GlobalExceptionHandlerMiddleware`.
- **GlobalExceptionHandlerMiddleware**: Middleware that catches all unhandled exceptions (including `DomainException` subclasses) and converts them into structured `ApiResponse` JSON error responses.
- **WorkService**: The backend microservice responsible for project management, stories, tasks, sprints, boards, reports, analytics, time tracking, cost rates, risk registers, workflows, labels, comments, search, saved filters, story templates, and export functionality.
- **Controller**: An ASP.NET Core API controller that receives HTTP requests, extracts context from JWT claims, delegates to a service method, and converts the `ServiceResult` to an HTTP response.
- **Service_Interface**: A C# interface in `WorkService.Domain` that defines the contract for a service, specifying method signatures and return types.
- **Service_Implementation**: A C# class in `WorkService.Infrastructure` that implements a service interface, containing business logic, validation, and repository calls.
- **ErrorCodes**: A static class in `WorkService.Domain.Exceptions` containing all numeric error values and string error code constants used in `ServiceResult.Fail(...)` and `DomainException` subclasses.

## Requirements

### Requirement 1: Create ServiceResult Class

**User Story:** As a developer, I want a `ServiceResult<T>` class in the WorkService domain layer, so that service methods can return typed success/failure results instead of throwing exceptions for expected business failures.

#### Acceptance Criteria

1. THE ServiceResult class SHALL be located at `WorkService.Domain/Results/ServiceResult.cs` and reside in the `WorkService.Domain.Results` namespace.
2. THE ServiceResult class SHALL expose the following properties: `IsSuccess` (bool), `Data` (T?), `Message` (string?), `StatusCode` (int, default 200), `ErrorValue` (int?), and `ErrorCode` (string?).
3. THE ServiceResult class SHALL provide static factory methods `Ok(T data, string? message)`, `Created(T data, string? message)`, `NoContent(string? message)`, and `Fail(int errorValue, string errorCode, string message, int statusCode)`.
4. WHEN `Ok` is called, THE ServiceResult class SHALL return an instance with `IsSuccess = true` and `StatusCode = 200`.
5. WHEN `Created` is called, THE ServiceResult class SHALL return an instance with `IsSuccess = true` and `StatusCode = 201`.
6. WHEN `NoContent` is called, THE ServiceResult class SHALL return an instance with `IsSuccess = true`, `Data = default`, and `StatusCode = 204`.
7. WHEN `Fail` is called, THE ServiceResult class SHALL return an instance with `IsSuccess = false`, `Data = default`, and the provided `errorValue`, `errorCode`, `message`, and `statusCode`.
8. THE ServiceResult class SHALL match the structure and API of `BillingService.Domain.Results.ServiceResult<T>` exactly.

### Requirement 2: Create ToActionResult Extension Method

**User Story:** As a developer, I want a `ToActionResult()` extension method for `ServiceResult<T>`, so that controllers can convert service results into properly formatted HTTP responses in a single call.

#### Acceptance Criteria

1. THE ToActionResult extension SHALL be located at `WorkService.Api/Extensions/ServiceResultExtensions.cs` and reside in the `WorkService.Api.Extensions` namespace.
2. WHEN a successful ServiceResult is converted, THE ToActionResult extension SHALL wrap the data in an `ApiResponse<T>.Ok(...)` envelope and return an `ObjectResult` with the ServiceResult StatusCode.
3. WHEN a failed ServiceResult is converted, THE ToActionResult extension SHALL wrap the error in an `ApiResponse<object>.Fail(...)` envelope and return an `ObjectResult` with the ServiceResult StatusCode.
4. WHEN a null ServiceResult is converted, THE ToActionResult extension SHALL return a 500 `ObjectResult` with error code `INTERNAL_ERROR`.
5. THE ToActionResult extension SHALL inject the `CorrelationId` from `HttpContext.Items["CorrelationId"]` into the response envelope.
6. THE ToActionResult extension SHALL accept an optional `HttpContext` parameter for correlation ID injection.
7. THE ToActionResult extension SHALL match the behavior of `BillingService.Api.Extensions.ServiceResultExtensions.ToActionResult` exactly.

### Requirement 3: Migrate Project Service Interfaces and Implementations

**User Story:** As a developer, I want the ProjectService to return `ServiceResult<T>` from all methods, so that project-related business failures are expressed as result values instead of thrown exceptions.

#### Acceptance Criteria

1. THE IProjectService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` with appropriate typed DTOs for each method.
2. WHEN a project is created successfully, THE ProjectService SHALL return `ServiceResult.Created(projectDetailResponse, "Project created successfully.")`.
3. WHEN a project is not found, THE ProjectService SHALL return `ServiceResult.Fail(ErrorCodes.ProjectNotFoundValue, ErrorCodes.ProjectNotFound, message, 404)` instead of throwing `ProjectNotFoundException`.
4. WHEN a project key has an invalid format, THE ProjectService SHALL return `ServiceResult.Fail(ErrorCodes.ProjectKeyInvalidFormatValue, ErrorCodes.ProjectKeyInvalidFormat, message, 400)` instead of throwing `ProjectKeyInvalidFormatException`.
5. WHEN a duplicate project key is detected, THE ProjectService SHALL return `ServiceResult.Fail(ErrorCodes.ProjectKeyDuplicateValue, ErrorCodes.ProjectKeyDuplicate, message, 409)` instead of throwing `ProjectKeyDuplicateException`.
6. WHEN a duplicate project name is detected, THE ProjectService SHALL return `ServiceResult.Fail(ErrorCodes.ProjectNameDuplicateValue, ErrorCodes.ProjectNameDuplicate, message, 409)` instead of throwing `ProjectNameDuplicateException`.
7. WHEN a project key change is attempted on a project with existing stories, THE ProjectService SHALL return `ServiceResult.Fail(ErrorCodes.ProjectKeyImmutableValue, ErrorCodes.ProjectKeyImmutable, message, 400)` instead of throwing `ProjectKeyImmutableException`.
8. WHEN `UpdateStatusAsync` completes successfully, THE ProjectService SHALL return `ServiceResult.NoContent("Project status updated.")`.

### Requirement 4: Migrate Story Service Interfaces and Implementations

**User Story:** As a developer, I want the StoryService to return `ServiceResult<T>` from all methods, so that story-related business failures are expressed as result values instead of thrown exceptions.

#### Acceptance Criteria

1. THE IStoryService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` with appropriate typed DTOs, and all `Task` (void) return types to `Task<ServiceResult<T>>` where a result is meaningful.
2. WHEN a story is not found, THE StoryService SHALL return `ServiceResult.Fail` with error code `STORY_NOT_FOUND` and status 404 instead of throwing `StoryNotFoundException`.
3. WHEN an invalid story status transition is attempted, THE StoryService SHALL return `ServiceResult.Fail` with error code `INVALID_STORY_TRANSITION` and status 400 instead of throwing `InvalidStoryTransitionException`.
4. WHEN a story requires an assignee before transition, THE StoryService SHALL return `ServiceResult.Fail` with error code `STORY_REQUIRES_ASSIGNEE` and status 400 instead of throwing `StoryRequiresAssigneeException`.
5. WHEN a bulk operation is performed, THE StoryService SHALL return `ServiceResult.Ok` with a response containing the count of successfully updated items and any individual failures.
6. WHEN a void operation (DeleteAsync, CreateLinkAsync, DeleteLinkAsync, ApplyLabelAsync, RemoveLabelAsync, UnassignAsync) completes successfully, THE StoryService SHALL return `ServiceResult.NoContent(message)`.

### Requirement 5: Migrate Task Service Interfaces and Implementations

**User Story:** As a developer, I want the TaskService to return `ServiceResult<T>` from all methods, so that task-related business failures are expressed as result values instead of thrown exceptions.

#### Acceptance Criteria

1. THE ITaskService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` with appropriate typed DTOs, and all `Task` (void) return types to `Task<ServiceResult<T>>`.
2. WHEN a task is not found, THE TaskService SHALL return `ServiceResult.Fail` with error code `TASK_NOT_FOUND` and status 404 instead of throwing `TaskNotFoundException`.
3. WHEN an invalid task status transition is attempted, THE TaskService SHALL return `ServiceResult.Fail` with error code `INVALID_TASK_TRANSITION` and status 400 instead of throwing `InvalidTaskTransitionException`.
4. WHEN an assignee is not in the required department, THE TaskService SHALL return `ServiceResult.Fail` with error code `ASSIGNEE_NOT_IN_DEPARTMENT` and status 400 instead of throwing `AssigneeNotInDepartmentException`.
5. WHEN an assignee is at capacity, THE TaskService SHALL return `ServiceResult.Fail` with error code `ASSIGNEE_AT_CAPACITY` and status 400 instead of throwing `AssigneeAtCapacityException`.
6. WHEN a void operation (DeleteAsync, UnassignAsync, LogHoursAsync) completes successfully, THE TaskService SHALL return `ServiceResult.NoContent(message)`.

### Requirement 6: Migrate Sprint Service Interfaces and Implementations

**User Story:** As a developer, I want the SprintService to return `ServiceResult<T>` from all methods, so that sprint-related business failures are expressed as result values instead of thrown exceptions.

#### Acceptance Criteria

1. THE ISprintService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` with appropriate typed DTOs, and all `Task` (void) return types to `Task<ServiceResult<T>>`.
2. WHEN a sprint is not found, THE SprintService SHALL return `ServiceResult.Fail` with error code `SPRINT_NOT_FOUND` and status 404 instead of throwing `SprintNotFoundException`.
3. WHEN sprint dates overlap with an existing sprint, THE SprintService SHALL return `ServiceResult.Fail` with error code `SPRINT_OVERLAP` and status 409 instead of throwing `SprintOverlapException`.
4. WHEN a sprint is already active, THE SprintService SHALL return `ServiceResult.Fail` with error code `SPRINT_ALREADY_ACTIVE` and status 400 instead of throwing `SprintAlreadyActiveException`.
5. WHEN a sprint is already completed, THE SprintService SHALL return `ServiceResult.Fail` with error code `SPRINT_ALREADY_COMPLETED` and status 400 instead of throwing `SprintAlreadyCompletedException`.
6. WHEN a void operation (AddStoryAsync, RemoveStoryAsync) completes successfully, THE SprintService SHALL return `ServiceResult.NoContent(message)`.

### Requirement 7: Migrate Comment Service Interfaces and Implementations

**User Story:** As a developer, I want the CommentService to return `ServiceResult<T>` from all methods, so that comment-related business failures are expressed as result values instead of thrown exceptions.

#### Acceptance Criteria

1. THE ICommentService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` with appropriate typed DTOs, and all `Task` (void) return types to `Task<ServiceResult<T>>`.
2. WHEN a comment is not found, THE CommentService SHALL return `ServiceResult.Fail` with error code `COMMENT_NOT_FOUND` and status 404 instead of throwing `CommentNotFoundException`.
3. WHEN a non-author attempts to edit a comment, THE CommentService SHALL return `ServiceResult.Fail` with error code `COMMENT_NOT_AUTHOR` and status 403 instead of throwing `CommentNotAuthorException`.
4. WHEN DeleteAsync completes successfully, THE CommentService SHALL return `ServiceResult.NoContent("Comment deleted.")`.

### Requirement 8: Migrate Label Service Interfaces and Implementations

**User Story:** As a developer, I want the LabelService to return `ServiceResult<T>` from all methods, so that label-related business failures are expressed as result values instead of thrown exceptions.

#### Acceptance Criteria

1. THE ILabelService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` with appropriate typed DTOs, and all `Task` (void) return types to `Task<ServiceResult<T>>`.
2. WHEN a label name already exists in the organization, THE LabelService SHALL return `ServiceResult.Fail` with error code `LABEL_NAME_DUPLICATE` and status 409 instead of throwing `LabelNameDuplicateException`.
3. WHEN a label is not found, THE LabelService SHALL return `ServiceResult.Fail` with error code `LABEL_NOT_FOUND` and status 404 instead of throwing `LabelNotFoundException`.
4. WHEN DeleteAsync completes successfully, THE LabelService SHALL return `ServiceResult.NoContent("Label deleted.")`.

### Requirement 9: Migrate Search, Board, and Report Services

**User Story:** As a developer, I want the SearchService, BoardService, and ReportService to return `ServiceResult<T>` from all methods, so that read-oriented services follow the same pattern as write-oriented services.

#### Acceptance Criteria

1. THE ISearchService interface SHALL change `Task<object>` to `Task<ServiceResult<T>>` for the `SearchAsync` method.
2. WHEN a search query is too short, THE SearchService SHALL return `ServiceResult.Fail` with error code `SEARCH_QUERY_TOO_SHORT` and status 400 instead of throwing `SearchQueryTooShortException`.
3. THE IBoardService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` for all four board methods.
4. THE IReportService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` for all five report methods.
5. WHEN board or report data is retrieved successfully, THE BoardService and ReportService SHALL return `ServiceResult.Ok(data, message)`.

### Requirement 10: Migrate Workflow Service Interfaces and Implementations

**User Story:** As a developer, I want the WorkflowService to return `ServiceResult<T>` from all methods, so that workflow configuration operations follow the ServiceResult pattern.

#### Acceptance Criteria

1. THE IWorkflowService interface SHALL change `Task<object>` to `Task<ServiceResult<T>>` for `GetWorkflowsAsync`, and `Task` (void) to `Task<ServiceResult<T>>` for `SaveOrganizationOverrideAsync` and `SaveDepartmentOverrideAsync`.
2. WHEN a workflow override is saved successfully, THE WorkflowService SHALL return `ServiceResult.NoContent("Workflow override saved.")`.
3. WHEN workflows are retrieved successfully, THE WorkflowService SHALL return `ServiceResult.Ok(data, "Workflows retrieved.")`.

### Requirement 11: Migrate Time Tracking Services

**User Story:** As a developer, I want the TimeEntryService, TimePolicyService, CostRateService, and TimerSessionService to return `ServiceResult<T>` from all methods, so that time tracking operations follow the ServiceResult pattern.

#### Acceptance Criteria

1. THE ITimeEntryService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` and all `Task` (void) return types to `Task<ServiceResult<T>>`.
2. WHEN a time entry is not found, THE TimeEntryService SHALL return `ServiceResult.Fail` with error code `TIME_ENTRY_NOT_FOUND` and status 404 instead of throwing `TimeEntryNotFoundException`.
3. WHEN daily hours are exceeded, THE TimeEntryService SHALL return `ServiceResult.Fail` with error code `DAILY_HOURS_EXCEEDED` and status 400 instead of throwing `DailyHoursExceededException`.
4. THE ITimePolicyService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>`.
5. THE ICostRateService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` and all `Task` (void) return types to `Task<ServiceResult<T>>`.
6. WHEN a duplicate cost rate is detected, THE CostRateService SHALL return `ServiceResult.Fail` with error code `COST_RATE_DUPLICATE` and status 409 instead of throwing `CostRateDuplicateException`.
7. THE ITimerSessionService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>`.
8. WHEN a timer is already active, THE TimerSessionService SHALL return `ServiceResult.Fail` with error code `TIMER_ALREADY_ACTIVE` and status 400 instead of throwing `TimerAlreadyActiveException`.
9. WHEN no active timer exists for stop, THE TimerSessionService SHALL return `ServiceResult.Fail` with error code `NO_ACTIVE_TIMER` and status 404 instead of throwing `NoActiveTimerException`.

### Requirement 12: Migrate Analytics and Risk Register Services

**User Story:** As a developer, I want the AnalyticsService, AnalyticsSnapshotService, CostSnapshotService, and RiskRegisterService to return `ServiceResult<T>` from all methods, so that analytics and risk operations follow the ServiceResult pattern.

#### Acceptance Criteria

1. THE IAnalyticsService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` and all `Task` (void) return types to `Task<ServiceResult<T>>`.
2. WHEN an invalid analytics parameter is provided, THE AnalyticsService SHALL return `ServiceResult.Fail` with error code `INVALID_ANALYTICS_PARAMETER` and status 400 instead of throwing `InvalidAnalyticsParameterException`.
3. WHEN a snapshot generation fails, THE AnalyticsService SHALL return `ServiceResult.Fail` with error code `SNAPSHOT_GENERATION_FAILED` and status 500 instead of throwing `SnapshotGenerationFailedException`.
4. THE IAnalyticsSnapshotService interface SHALL change `Task<object>` to `Task<ServiceResult<T>>` for `GetSnapshotStatusAsync`, and `Task` (void) to `Task<ServiceResult<T>>` for `TriggerSprintCloseSnapshotsAsync` and `GeneratePeriodicSnapshotsAsync`.
5. THE ICostSnapshotService interface SHALL change `Task<object>` to `Task<ServiceResult<T>>` for `ListByProjectAsync`.
6. THE IRiskRegisterService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` and all `Task` (void) return types to `Task<ServiceResult<T>>`.
7. WHEN a risk is not found, THE RiskRegisterService SHALL return `ServiceResult.Fail` with error code `RISK_NOT_FOUND` and status 404 instead of throwing `RiskNotFoundException`.

### Requirement 13: Migrate Story Template and Export Services

**User Story:** As a developer, I want the StoryTemplateService and ExportService to return `ServiceResult<T>` from all methods, so that template and export operations follow the ServiceResult pattern.

#### Acceptance Criteria

1. THE IStoryTemplateService interface SHALL change all `Task<object>` return types to `Task<ServiceResult<T>>` and all `Task` (void) return types to `Task<ServiceResult<T>>`.
2. WHEN a story template is created successfully, THE StoryTemplateService SHALL return `ServiceResult.Created(data, "Story template created.")`.
3. WHEN DeleteAsync completes successfully, THE StoryTemplateService SHALL return `ServiceResult.NoContent("Story template deleted.")`.
4. THE IExportService interface SHALL change `Task<byte[]>` return types to `Task<ServiceResult<byte[]>>` for both export methods.
5. WHEN an export is generated successfully, THE ExportService SHALL return `ServiceResult.Ok(csvBytes, "Export generated.")`.

### Requirement 14: Migrate All Controllers to One-Liner Pattern

**User Story:** As a developer, I want all 18 WorkService controllers to use the `result.ToActionResult(HttpContext)` one-liner pattern, so that controllers contain no business logic, no manual `ApiResponse` construction, and no try/catch blocks.

#### Acceptance Criteria

1. WHEN a controller action calls a service method, THE Controller SHALL use the pattern `return (await _service.Method(...)).ToActionResult(HttpContext);` as a single expression.
2. THE Controller SHALL remove all manual `ApiResponse<object>.Ok(result, message).ToActionResult(HttpContext, statusCode)` constructions.
3. THE Controller SHALL not contain any try/catch blocks for service calls.
4. THE Controller SHALL not contain any business logic or conditional error handling.
5. WHEN a service method returns `ServiceResult` with `StatusCode = 201`, THE Controller SHALL rely on `ToActionResult` to set the correct HTTP 201 status code without passing an explicit status code parameter.
6. THE SavedFilterController SHALL be refactored to use a SavedFilterService (or equivalent service layer) instead of calling the repository directly, so that the controller follows the same thin delegation pattern as all other controllers.

### Requirement 15: Preserve DomainException for Invariant Violations

**User Story:** As a developer, I want DomainExceptions to remain in use for truly unexpected invariant violations, so that errors thrown from deep in the call stack (repositories, validators, internal helpers) are still caught by the GlobalExceptionHandlerMiddleware.

#### Acceptance Criteria

1. WHILE a DomainException is thrown from a repository or internal helper, THE GlobalExceptionHandlerMiddleware SHALL continue to catch the exception and return a structured `ApiResponse` error response.
2. THE migration SHALL only convert service-level exceptions (thrown directly in service implementations for expected business failures) to `ServiceResult.Fail(...)`.
3. THE migration SHALL preserve all existing DomainException subclasses in `WorkService.Domain.Exceptions` for use by repositories and deep call-stack code.
4. IF a service method calls a repository that throws a DomainException for a constraint violation, THEN THE service method SHALL allow the exception to propagate to the middleware rather than catching and converting it to ServiceResult.

### Requirement 16: Incremental Migration with Test Continuity

**User Story:** As a developer, I want the migration to proceed incrementally (service by service), so that existing tests continue to pass after each individual service migration and the system remains deployable at every step.

#### Acceptance Criteria

1. THE migration SHALL be performed one service at a time, with each service migration including the interface change, implementation change, controller change, and test update as a single atomic unit.
2. WHEN a service is migrated, THE existing unit tests for that service SHALL be updated to assert on `ServiceResult` properties (`IsSuccess`, `Data`, `StatusCode`, `ErrorCode`) instead of catching thrown exceptions.
3. WHEN a service is migrated, THE existing unit tests SHALL continue to pass without modification to test logic that is unrelated to the return type change.
4. IF a test previously asserted that a service method throws a specific DomainException, THEN THE test SHALL be updated to assert that the service method returns a `ServiceResult` with `IsSuccess = false` and the corresponding `ErrorCode`.
5. THE StoryIdGenerator interface SHALL remain unchanged because the method returns a typed tuple `(string StoryKey, long SequenceNumber)` and is an internal utility that does not participate in the controller response flow.
6. THE ActivityLogService `LogAsync` method SHALL remain unchanged because the method is a fire-and-forget internal operation that does not return data to a controller.
