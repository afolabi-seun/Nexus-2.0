# Requirements Document — WorkService

## Introduction

This document defines the complete requirements for the Nexus-2.0 WorkService — the core Agile workflow management microservice of the Enterprise Agile Platform. WorkService runs on port 5003 with database `nexus_work` and follows Clean Architecture (.NET 8) with Domain / Application / Infrastructure / Api layers.

WorkService manages the entire Agile lifecycle: projects as organizational containers, stories with professional IDs (e.g., `NEXUS-42`), tasks with department-based assignment, sprints, board views (Kanban, Sprint, Department, Backlog), comments with @mentions, labels, activity feeds, full-text search, reports, and workflow customization. It is the largest and most feature-rich service in the platform.

The scoping model is: Organization → Projects → Stories/Sprints. Each organization can have multiple projects, and each project has its own backlog, sprints, and story key prefix (e.g., `NEXUS`, `ACME`). Stories and sprints are scoped to a project within an organization.

WorkService depends on ProfileService for organization settings (sprint duration), team member lookup, and department member lists. It depends on SecurityService for service-to-service JWT issuance. It publishes audit events and notifications to `outbox:work` for UtilityService processing.

All requirements are derived from the platform documentation:
- `docs/nexus-2.0-backend-requirements.md` (REQ-036 – REQ-070, REQ-086 – REQ-108)
- `docs/nexus-2.0-backend-specification.md` (WorkService specification, sections 6.1–6.12)
- `.kiro/specs/security-service/requirements.md` (SecurityService integration points)
- `.kiro/specs/profile-service/requirements.md` (ProfileService integration points)

## Glossary

- **WorkService**: Microservice (port 5003, database `nexus_work`) responsible for story management, task management, sprint management, board views, comments, labels, activity feeds, search, reports, and workflow customization.
- **ProfileService**: Microservice (port 5002) that owns TeamMember, Organization, and Department records. WorkService calls ProfileService for organization settings (sprint duration), team member lookup, and department member lists.
- **SecurityService**: Microservice (port 5001) responsible for authentication, JWT issuance, and RBAC. WorkService calls SecurityService for service-to-service JWT issuance.
- **UtilityService**: Microservice (port 5200) responsible for audit logging, error logging, notifications, and reference data. WorkService publishes events to `outbox:work` for UtilityService processing.
- **Project**: An organizational container within an organization that groups stories and sprints. Has a unique `ProjectKey` (2–10 uppercase alphanumeric, e.g., `NEXUS`, `ACME`) used as the story key prefix. The scoping model is Organization → Projects → Stories/Sprints.
- **ProjectKey**: The unique identifier prefix for a project, used to generate story keys in the format `{ProjectKey}-{SequenceNumber}`. Must be 2–10 uppercase alphanumeric characters, globally unique, and immutable once stories exist in the project.
- **Story**: A work item representing a feature, bug, improvement, or technical debt. Belongs to a project. Has a professional ID (e.g., `NEXUS-42`) and follows a defined workflow state machine: Backlog → Ready → InProgress → InReview → QA → Done → Closed.
- **Story_Key**: The human-readable professional ID for a story in the format `{ProjectKey}-{SequenceNumber}` (e.g., `NEXUS-42`). Generated atomically using a PostgreSQL sequence table keyed by project.
- **Task**: An actionable work item within a story. Assigned to a specific department based on task type. Follows its own workflow state machine: ToDo → InProgress → InReview → Done.
- **Sprint**: A time-boxed iteration (1–4 weeks) containing stories. Belongs to a project. Follows lifecycle: Planning → Active → Completed (or Cancelled). Only one sprint can be active per project at a time.
- **SprintStory**: Junction entity linking a Story to a Sprint, with `AddedDate` and optional `RemovedDate`.
- **Comment**: A threaded discussion entry on a story or task, supporting @mentions and Markdown content. Only the author can edit; author or OrgAdmin can delete.
- **Activity_Log**: An immutable record of changes to stories and tasks — status transitions, assignments, edits, comments, label changes, sprint changes. Provides a complete timeline for each entity.
- **Label**: An organization-scoped tag applied to stories for categorization and filtering. Maximum 10 labels per story.
- **StoryLabel**: Junction entity linking a Story to a Label.
- **Story_Link**: A bidirectional relationship between two stories. Link types: `blocks`, `is_blocked_by`, `relates_to`, `duplicates`.
- **Story_Points**: Fibonacci-scale estimation (1, 2, 3, 5, 8, 13, 21) representing relative effort.
- **Priority**: Story or task urgency level — Critical, High, Medium, Low.
- **Task_Type**: Classification of a task that determines department routing — Development (→ Engineering), Testing (→ QA), DevOps (→ DevOps), Design (→ Design), Documentation (→ Product), Bug (→ Engineering).
- **Velocity**: Sum of completed story points in a sprint. Calculated when a sprint is completed.
- **Burndown**: Daily data showing remaining story points vs. ideal linear decrease over a sprint duration.
- **Board**: A structured data view of work items — Kanban (stories by status), Sprint Board (tasks by status for active sprint), Department Board (tasks by department), Backlog (unassigned stories by priority).
- **Saved_Filter**: A user-saved filter configuration for board views and search, stored as JSON.
- **Story_Sequence**: Per-project PostgreSQL table maintaining an atomic auto-incrementing counter for story key generation.
- **Professional_ID**: The `{ProjectKey}-{SequenceNumber}` format used for stories. ProjectKey is 2–10 uppercase alphanumeric characters, configured per project.
- **Organization**: Top-level tenant entity. All WorkService data is scoped to an organization via `OrganizationId`.
- **Department**: Functional unit within an organization. Tasks are routed to departments based on task type.
- **TeamMember**: A user within an organization. WorkService references team members for story/task assignment, comments, and activity tracking.
- **FlgStatus**: Soft-delete lifecycle field — `A` (Active), `D` (Deleted/Deactivated).
- **ApiResponse**: Standardized JSON envelope `ApiResponse<T>` with `ResponseCode`, `Success`, `Data`, `ErrorCode`, `CorrelationId`, `Errors` fields.
- **DomainException**: Base exception class for business rule violations, containing `ErrorValue`, `ErrorCode`, `StatusCode`, and `CorrelationId`.
- **CorrelationId**: End-to-end trace identifier (`X-Correlation-Id` header) propagated across all service calls and included in all API responses.
- **Outbox**: Redis-based async messaging pattern. WorkService publishes audit events and notifications to `outbox:work`. UtilityService polls and processes the queue.
- **IOrganizationEntity**: Marker interface for entities scoped to an organization, enabling EF Core global query filters by `OrganizationId`.
- **Polly**: .NET resilience library used for retry (3x exponential), circuit breaker (5 failures / 30s), and timeout (10s) on inter-service calls.
- **Clean_Architecture**: Four-layer architecture — Domain (entities, interfaces), Application (DTOs, validators), Infrastructure (EF Core, Redis, HTTP clients), Api (controllers, middleware).
- **Service_JWT**: Short-lived JWT for inter-service communication, issued by SecurityService and cached in Redis. Used by WorkService when calling ProfileService and SecurityService.
- **Workflow_State_Machine**: Static validation logic that enforces valid status transitions for stories and tasks. Defined in `WorkflowStateMachine` helper class in the Domain layer.

## Requirements

### Requirement 1: Professional Story ID Generation (REQ-036)

**User Story:** As a team member, I want stories to have professional, human-readable IDs so that they are easy to reference in conversations and documentation.

#### Acceptance Criteria

1. WHEN a story is created, THE WorkService SHALL generate a story key in the format `{ProjectKey}-{SequenceNumber}` (e.g., `NEXUS-1`, `NEXUS-42`, `ACME-100`), where `ProjectKey` is the key of the project the story belongs to.
2. WHEN the project key is needed, THE WorkService SHALL check Redis cache (`project_prefix:{projectId}`, 60-minute TTL) first. IF the cache misses, THEN THE WorkService SHALL look up the project's `ProjectKey` from the database and cache the result.
3. WHEN the sequence number is generated, THE WorkService SHALL use an atomic PostgreSQL `UPDATE ... RETURNING` on the `story_sequence` table (keyed by `ProjectId`), ensuring gap-free, monotonically increasing IDs even under concurrent creation.
4. WHEN the project's first story is created, THE WorkService SHALL initialize the `story_sequence` row via `INSERT ... ON CONFLICT DO NOTHING` before the increment.
5. IF sequence initialization fails, THEN THE WorkService SHALL return HTTP 500 with `STORY_SEQUENCE_INIT_FAILED` (4034).
6. THE WorkService SHALL enforce a unique index on `(ProjectId, StoryKey)` to guarantee story key uniqueness per project.

### Requirement 2: Project Management

**User Story:** As an OrgAdmin or DeptLead, I want to create and manage projects within an organization so that work is organized into separate backlogs with distinct story key prefixes.

#### Acceptance Criteria

1. WHEN `POST /api/v1/projects` is called with `{projectName, projectKey, description, leadId}`, THE WorkService SHALL create the project with `FlgStatus=A`, set `OrganizationId` from the authenticated user's context, and return HTTP 201 with the full project.
2. WHEN a project is created, THE WorkService SHALL validate that `ProjectKey` is 2–10 uppercase alphanumeric characters matching `^[A-Z0-9]{2,10}$`. IF the format is invalid, THEN THE WorkService SHALL return HTTP 400 with `PROJECT_KEY_INVALID_FORMAT` (4045).
3. WHEN a project is created, THE WorkService SHALL validate that `ProjectKey` is globally unique across all organizations. IF the key already exists, THEN THE WorkService SHALL return HTTP 409 with `PROJECT_KEY_DUPLICATE` (4043).
4. WHEN a project is created, THE WorkService SHALL validate that `ProjectName` is unique within the organization. IF the name already exists, THEN THE WorkService SHALL return HTTP 409 with `PROJECT_NAME_DUPLICATE` (4042).
5. WHEN `GET /api/v1/projects/{id}` is called, THE WorkService SHALL return the project detail including story count, sprint count, and lead info.
6. WHEN `GET /api/v1/projects` is called, THE WorkService SHALL return a paginated list of projects for the organization, filterable by status.
7. WHEN `PUT /api/v1/projects/{id}` is called, THE WorkService SHALL update the project fields (`ProjectName`, `Description`, `LeadId`). THE WorkService SHALL not allow updating `ProjectKey` if stories exist in the project. IF `ProjectKey` update is attempted when stories exist, THEN THE WorkService SHALL return HTTP 400 with `PROJECT_KEY_IMMUTABLE` (4044).
8. WHEN `PATCH /api/v1/projects/{id}/status` is called, THE WorkService SHALL update the project's `FlgStatus`.
9. WHEN a project is created, THE WorkService SHALL restrict creation to users with `OrgAdmin` or `DeptLead` roles. IF the user lacks the required role, THEN THE WorkService SHALL return HTTP 403 with `INSUFFICIENT_PERMISSIONS` (4032).
10. IF the project does not exist, THEN THE WorkService SHALL return HTTP 404 with `PROJECT_NOT_FOUND` (4041).
11. THE WorkService SHALL enforce a unique index on `ProjectKey` (globally unique) and a unique index on `(OrganizationId, ProjectName)`.

### Requirement 3: Story CRUD Operations (REQ-037)

**User Story:** As a team member, I want to create, read, update, and delete stories so that I can manage the product backlog.

#### Acceptance Criteria

1. WHEN `POST /api/v1/stories` is called with `{projectId, title, description, acceptanceCriteria, priority, storyPoints, departmentId}`, THE WorkService SHALL validate that the project exists and belongs to the authenticated user's organization, generate a story key using the project's `ProjectKey`, set status to `Backlog`, set `ReporterId` to the authenticated user, and return HTTP 201 with the full story including `StoryKey`.
2. WHEN `GET /api/v1/stories/{id}` is called, THE WorkService SHALL return the story detail including project info, all tasks, comments count, labels, activity log, assignee info, linked stories, and completion percentage (based on task completion).
3. WHEN `GET /api/v1/stories` is called, THE WorkService SHALL return a paginated list of stories, filterable by projectId, status, priority, department, assignee, sprint, labels, and date range.
4. WHEN `GET /api/v1/stories/by-key/{storyKey}` is called (e.g., `NEXUS-42`), THE WorkService SHALL resolve the story by key and return the detail. IF the key does not resolve, THEN THE WorkService SHALL return HTTP 404 with `STORY_KEY_NOT_FOUND` (4020).
5. WHEN `PUT /api/v1/stories/{id}` is called, THE WorkService SHALL update the story fields and record changes in the activity log.
6. WHEN `DELETE /api/v1/stories/{id}` is called (soft delete, sets `FlgStatus=D`), THE WorkService SHALL mark the story as deleted. IF the story is in an active sprint, THEN THE WorkService SHALL return HTTP 400 with `STORY_IN_ACTIVE_SPRINT` (4026).
7. WHEN story points are set, THE WorkService SHALL validate they are a Fibonacci number (1, 2, 3, 5, 8, 13, 21). IF the value is invalid, THEN THE WorkService SHALL return HTTP 400 with `INVALID_STORY_POINTS` (4023).
8. WHEN priority is set, THE WorkService SHALL validate it is one of: Critical, High, Medium, Low. IF the value is invalid, THEN THE WorkService SHALL return HTTP 400 with `INVALID_PRIORITY` (4024).
9. IF the story does not exist, THEN THE WorkService SHALL return HTTP 404 with `STORY_NOT_FOUND` (4001).

### Requirement 4: Story Workflow State Machine (REQ-038)

**User Story:** As a team member, I want stories to follow a defined workflow so that the development process is structured and trackable.

#### Acceptance Criteria

1. WHEN a story is created, THE WorkService SHALL set its status to `Backlog`.
2. WHEN `PATCH /api/v1/stories/{id}/status` is called with a new status, THE WorkService SHALL validate the transition against the state machine:

| From | To | Conditions |
|------|----|------------|
| Backlog | Ready | Story has title, description, and story points |
| Ready | InProgress | Story is assigned to a team member |
| InProgress | InReview | At least one task exists and all dev tasks are done |
| InReview | InProgress | Reviewer requests changes |
| InReview | QA | Reviewer approves |
| QA | InProgress | QA finds defects |
| QA | Done | All tasks complete, QA passed |
| Done | Closed | Stakeholder accepts |

3. IF an invalid transition is attempted, THEN THE WorkService SHALL return HTTP 400 with `INVALID_STORY_TRANSITION` (4004).
4. WHEN a story transitions to `Ready` without a description, THE WorkService SHALL return HTTP 400 with `STORY_DESCRIPTION_REQUIRED` (4039).
5. WHEN a story transitions to `Ready` without story points, THE WorkService SHALL return HTTP 400 with `STORY_REQUIRES_POINTS` (4015).
6. WHEN a story transitions to `InProgress` without an assignee, THE WorkService SHALL return HTTP 400 with `STORY_REQUIRES_ASSIGNEE` (4013).
7. WHEN a story transitions to `InReview` without tasks, THE WorkService SHALL return HTTP 400 with `STORY_REQUIRES_TASKS` (4014).
8. WHEN a story transitions to `Done`, THE WorkService SHALL set `CompletedDate` to `DateTime.UtcNow`.
9. WHEN a story status changes, THE WorkService SHALL create an activity log entry and publish a `StoryStatusChanged` notification to `outbox:work`.

### Requirement 5: Story Assignment and Department Tracking (REQ-039)

**User Story:** As a DeptLead, I want to assign stories to team members and track which departments contribute so that cross-department collaboration is visible.

#### Acceptance Criteria

1. WHEN a story is assigned to a team member via `PATCH /api/v1/stories/{id}/assign`, THE WorkService SHALL set the `AssigneeId` and publish a `StoryAssigned` notification to `outbox:work`.
2. WHEN a story has tasks assigned to multiple departments, THE WorkService SHALL include a `departmentContributions` field in the story detail response showing which departments have tasks and their completion status.
3. WHEN an OrgAdmin assigns a story, THE WorkService SHALL allow assignment to any department.
4. WHEN a DeptLead assigns a story, THE WorkService SHALL restrict assignment to members within the DeptLead's own department.
5. WHEN `PATCH /api/v1/stories/{id}/unassign` is called, THE WorkService SHALL clear the `AssigneeId` and create an activity log entry.

### Requirement 6: Story Linking (REQ-040)

**User Story:** As a team member, I want to link related stories so that dependencies and relationships are visible.

#### Acceptance Criteria

1. WHEN `POST /api/v1/stories/{id}/links` is called with `{targetStoryId, linkType}`, THE WorkService SHALL create a bidirectional link. Link types: `blocks`, `is_blocked_by`, `relates_to`, `duplicates`.
2. WHEN a story link is created, THE WorkService SHALL automatically create the inverse link (e.g., if A `blocks` B, then B `is_blocked_by` A).
3. WHEN `GET /api/v1/stories/{id}` is called, THE WorkService SHALL include linked stories in the response.
4. WHEN `DELETE /api/v1/stories/{id}/links/{linkId}` is called, THE WorkService SHALL remove both directions of the link.

### Requirement 7: Task CRUD Operations (REQ-041)

**User Story:** As a team member, I want to create and manage tasks within stories so that work is broken down into actionable items.

#### Acceptance Criteria

1. WHEN `POST /api/v1/tasks` is called with `{storyId, title, description, taskType, priority, estimatedHours}`, THE WorkService SHALL create the task with status `ToDo`, auto-map the department based on task type, and return HTTP 201.
2. WHEN the task type is not one of `Development`, `Testing`, `DevOps`, `Design`, `Documentation`, `Bug`, THE WorkService SHALL return HTTP 400 with `INVALID_TASK_TYPE` (4025).
3. WHEN `GET /api/v1/tasks/{id}` is called, THE WorkService SHALL return the task detail including parent story key, assignee info, department, and time tracking fields (`EstimatedHours`, `ActualHours`).
4. WHEN `GET /api/v1/stories/{storyId}/tasks` is called, THE WorkService SHALL return all tasks for the story.
5. WHEN `PUT /api/v1/tasks/{id}` is called, THE WorkService SHALL update the task and record changes in the activity log.
6. WHEN `DELETE /api/v1/tasks/{id}` is called (soft delete), THE WorkService SHALL mark the task as deleted. IF the task is in `InProgress` status, THEN THE WorkService SHALL return HTTP 400 with `TASK_IN_PROGRESS` (4027).
7. IF the task does not exist, THEN THE WorkService SHALL return HTTP 404 with `TASK_NOT_FOUND` (4002).

### Requirement 8: Task Department-Based Assignment (REQ-042)

**User Story:** As the platform, I want tasks to be automatically routed to the correct department based on task type so that work flows to the right team.

#### Acceptance Criteria

1. WHEN a task is created with a `TaskType`, THE WorkService SHALL auto-map the department using the following mapping:

| Task Type | Default Department Code |
|-----------|------------------------|
| Development | ENG |
| Testing | QA |
| DevOps | DEVOPS |
| Design | DESIGN |
| Documentation | PROD |
| Bug | ENG |

2. WHEN `GET /api/v1/tasks/suggest-assignee?taskType={type}&organizationId={orgId}` is called, THE WorkService SHALL suggest an assignee by selecting the available member in the mapped department with the lowest active task count who is under their `MaxConcurrentTasks` limit.
3. WHEN a task is assigned to a member not in the target department, THE WorkService SHALL return HTTP 400 with `ASSIGNEE_NOT_IN_DEPARTMENT` (4018).
4. WHEN a task is assigned to a member who has reached `MaxConcurrentTasks`, THE WorkService SHALL return HTTP 400 with `ASSIGNEE_AT_CAPACITY` (4019).
5. WHEN `PATCH /api/v1/tasks/{id}/assign` is called by a DeptLead, THE WorkService SHALL restrict assignment to members within the DeptLead's own department.
6. WHEN `PATCH /api/v1/tasks/{id}/self-assign` is called by a Member or higher role, THE WorkService SHALL set the `AssigneeId` to the authenticated user and publish a `TaskAssigned` notification to `outbox:work`.

### Requirement 9: Task Workflow State Machine (REQ-043)

**User Story:** As a team member, I want tasks to follow a defined workflow so that progress is tracked consistently.

#### Acceptance Criteria

1. WHEN a task is created, THE WorkService SHALL set its status to `ToDo`.
2. WHEN `PATCH /api/v1/tasks/{id}/status` is called with a new status, THE WorkService SHALL validate the transition against the state machine:

| From | To | Conditions |
|------|----|------------|
| ToDo | InProgress | Task has an assignee |
| InProgress | InReview | Assignee submits for review |
| InReview | InProgress | Reviewer requests changes |
| InReview | Done | Reviewer approves |

3. IF an invalid transition is attempted, THEN THE WorkService SHALL return HTTP 400 with `INVALID_TASK_TRANSITION` (4005).
4. WHEN a task transitions to `Done`, THE WorkService SHALL set `CompletedDate` to `DateTime.UtcNow`.
5. WHEN a task status changes, THE WorkService SHALL create an activity log entry and publish a `TaskStatusChanged` notification to `outbox:work`.

### Requirement 10: Task Time Tracking (REQ-044)

**User Story:** As a team member, I want to log time against tasks so that effort is tracked accurately.

#### Acceptance Criteria

1. WHEN `PATCH /api/v1/tasks/{id}/log-hours` is called with `{hours, description}`, THE WorkService SHALL add the hours to `ActualHours` and record the time entry.
2. IF hours is zero or negative, THEN THE WorkService SHALL return HTTP 400 with `HOURS_MUST_BE_POSITIVE` (4035).
3. WHEN `GET /api/v1/tasks/{id}` is called, THE WorkService SHALL include `EstimatedHours`, `ActualHours`, and the list of time log entries in the response.

### Requirement 11: Story-Task Traceability (REQ-045)

**User Story:** As a project manager, I want complete traceability between stories and tasks so that I can track progress and department contributions.

#### Acceptance Criteria

1. WHEN `GET /api/v1/stories/{id}` is called, THE WorkService SHALL include: total task count, completed task count, completion percentage (`CompletedTasks / TotalTasks * 100`), department contribution breakdown (`{ departmentName, taskCount, completedTaskCount }`), and all tasks with their status, assignee, and department.
2. WHEN all tasks in a story reach `Done`, THE WorkService SHALL make the story eligible for the `QA → Done` transition.
3. WHEN a task's parent story is retrieved, THE WorkService SHALL include the story key in the task response.

### Requirement 12: Sprint CRUD Operations (REQ-046)

**User Story:** As a DeptLead or OrgAdmin, I want to create and manage sprints within a project so that work is organized into time-boxed iterations.

#### Acceptance Criteria

1. WHEN `POST /api/v1/projects/{projectId}/sprints` is called with `{sprintName, goal, startDate, endDate}`, THE WorkService SHALL create the sprint with status `Planning`, associate it with the specified project, and return HTTP 201.
2. WHEN `endDate` is before `startDate`, THE WorkService SHALL return HTTP 400 with `SPRINT_END_BEFORE_START` (4033).
3. WHEN `GET /api/v1/sprints/{id}` is called, THE WorkService SHALL return sprint details including project info, stories, metrics, and burndown data.
4. WHEN `GET /api/v1/sprints` is called, THE WorkService SHALL return sprints for the organization (paginated, filterable by status and projectId).
5. WHEN `PUT /api/v1/sprints/{id}` is called, THE WorkService SHALL update the sprint. THE WorkService SHALL only allow updates when the sprint is in `Planning` status.
6. WHEN the default sprint duration is not specified, THE WorkService SHALL use the organization's `DefaultSprintDurationWeeks` setting from ProfileService.
7. IF the sprint does not exist, THEN THE WorkService SHALL return HTTP 404 with `SPRINT_NOT_FOUND` (4003).

### Requirement 13: Sprint Lifecycle (REQ-047)

**User Story:** As a DeptLead, I want to manage the sprint lifecycle so that iterations are properly started, completed, and reviewed.

#### Acceptance Criteria

1. WHEN `PATCH /api/v1/sprints/{id}/start` is called, THE WorkService SHALL transition the sprint from `Planning` to `Active`.
2. WHEN a sprint is started and another sprint is already `Active` for the same project, THE WorkService SHALL return HTTP 400 with `ONLY_ONE_ACTIVE_SPRINT` (4016).
3. WHEN `PATCH /api/v1/sprints/{id}/complete` is called, THE WorkService SHALL transition the sprint from `Active` to `Completed`, calculate velocity (sum of story points for stories that reached `Done` or `Closed`), and move incomplete stories back to `Backlog` status with `SprintId` set to null.
4. WHEN `PATCH /api/v1/sprints/{id}/cancel` is called, THE WorkService SHALL transition the sprint to `Cancelled` and move all stories back to `Backlog`.
5. WHEN a sprint is already completed, THE WorkService SHALL return HTTP 400 with `SPRINT_ALREADY_COMPLETED` (4022).
6. WHEN a sprint is started, THE WorkService SHALL publish a `SprintStarted` notification to `outbox:work`.
7. WHEN a sprint is completed, THE WorkService SHALL publish a `SprintEnded` notification to `outbox:work` with `SprintName`, `Velocity`, and `CompletionRate`.

### Requirement 14: Sprint Planning — Story Assignment to Sprint (REQ-048)

**User Story:** As a DeptLead, I want to add and remove stories from a sprint so that the sprint backlog is properly planned.

#### Acceptance Criteria

1. WHEN `POST /api/v1/sprints/{sprintId}/stories` is called with `{storyId}`, THE WorkService SHALL validate that the story belongs to the same project as the sprint, add the story to the sprint (create `SprintStory` record), and set the story's `SprintId`.
2. WHEN stories are added to a sprint that is not in `Planning` status, THE WorkService SHALL return HTTP 400 with `SPRINT_NOT_IN_PLANNING` (4006).
3. WHEN a story is already in the sprint, THE WorkService SHALL return HTTP 409 with `STORY_ALREADY_IN_SPRINT` (4007).
4. WHEN `DELETE /api/v1/sprints/{sprintId}/stories/{storyId}` is called, THE WorkService SHALL remove the story from the sprint (set `RemovedDate` on `SprintStory`, clear story's `SprintId`).
5. WHEN a story not in the sprint is targeted for removal, THE WorkService SHALL return HTTP 400 with `STORY_NOT_IN_SPRINT` (4008).
6. WHEN a story from a different project is added to a sprint, THE WorkService SHALL return HTTP 400 with `STORY_PROJECT_MISMATCH` (4046).

### Requirement 15: Sprint Metrics and Burndown (REQ-049)

**User Story:** As a project manager, I want sprint metrics and burndown data so that I can track iteration progress.

#### Acceptance Criteria

1. WHEN `GET /api/v1/sprints/{id}/metrics` is called, THE WorkService SHALL return: `TotalStories`, `CompletedStories`, `TotalStoryPoints`, `CompletedStoryPoints`, `CompletionRate` (`CompletedStories / TotalStories * 100`), `Velocity` (`CompletedStoryPoints`), `StoriesByStatus` (dictionary of status → count), `TasksByDepartment` (dictionary of department → task count), and `BurndownData` (array of `{ date, remainingPoints, idealRemainingPoints }`).
2. WHEN burndown data is calculated, THE WorkService SHALL compute `IdealRemainingPoints` as a linear decrease from total points to 0 over the sprint duration, and `RemainingPoints` as total points minus completed points as of each day.
3. WHEN sprint metrics are requested, THE WorkService SHALL cache results in Redis (`sprint_metrics:{sprintId}`, 5-minute TTL).

### Requirement 16: Sprint Velocity Tracking (REQ-050)

**User Story:** As a project manager, I want to track velocity across sprints so that I can forecast future capacity.

#### Acceptance Criteria

1. WHEN `GET /api/v1/sprints/velocity` is called with optional `count` parameter, THE WorkService SHALL return velocity data for the last N completed sprints (default 10), each with `SprintName`, `Velocity`, `StartDate`, `EndDate`.
2. WHEN a sprint is completed, THE WorkService SHALL calculate and store the velocity in the `Velocity` field.
3. WHEN velocity history is requested, THE WorkService SHALL sort results by sprint end date descending.

### Requirement 17: Kanban Board View (REQ-051)

**User Story:** As a team member, I want a kanban board view so that I can visualize work in progress.

#### Acceptance Criteria

1. WHEN `GET /api/v1/boards/kanban` is called with optional `sprintId` and optional `projectId`, THE WorkService SHALL return stories grouped by workflow status columns.
2. WHEN a `projectId` is provided, THE WorkService SHALL show only stories in that project. WHEN a `sprintId` is provided, THE WorkService SHALL show only stories in that sprint. WHEN both are omitted, THE WorkService SHALL show all active stories for the organization.
3. WHEN the board is returned, THE WorkService SHALL include for each column: `Status`, `CardCount`, `TotalPoints`, and an array of `KanbanCard` objects with `StoryKey`, `Title`, `Priority`, `StoryPoints`, `AssigneeName`, `AssigneeAvatarUrl`, `Labels`, `TaskCount`, `CompletedTaskCount`, `ProjectName`.
4. WHEN board data is requested, THE WorkService SHALL cache results in Redis (`board_kanban:{organizationId}:{projectId}:{sprintId}`, 2-minute TTL).
5. WHEN the board is filtered, THE WorkService SHALL support filtering by project, department, assignee, priority, and labels.

### Requirement 18: Sprint Board View (REQ-052)

**User Story:** As a team member, I want a sprint board showing the current sprint's tasks grouped by status so that I can track daily progress.

#### Acceptance Criteria

1. WHEN `GET /api/v1/boards/sprint` is called with optional `projectId`, THE WorkService SHALL return the active sprint's tasks for the specified project (or the organization's first active sprint if omitted) grouped by task status columns (ToDo, InProgress, InReview, Done).
2. WHEN no sprint is active for the specified project, THE WorkService SHALL return an empty board with a message indicating no active sprint.
3. WHEN the sprint board is returned, THE WorkService SHALL include for each card: `StoryKey`, `TaskTitle`, `TaskType`, `AssigneeName`, `DepartmentName`, `Priority`, `ProjectName`.

### Requirement 19: Backlog View (REQ-053)

**User Story:** As a product owner, I want a backlog view showing prioritized stories not in any sprint so that I can plan upcoming work.

#### Acceptance Criteria

1. WHEN `GET /api/v1/boards/backlog` is called with optional `projectId`, THE WorkService SHALL return stories where `SprintId IS NULL`, filtered by project if specified, sorted by priority (Critical > High > Medium > Low) then by `DateCreated`.
2. WHEN the backlog is returned, THE WorkService SHALL include `TotalStories`, `TotalPoints`, and an array of `BacklogItem` objects with `StoryKey`, `Title`, `Priority`, `StoryPoints`, `Status`, `AssigneeName`, `Labels`, `TaskCount`, `DateCreated`, `ProjectName`.
3. WHEN backlog data is requested, THE WorkService SHALL cache results in Redis (`board_backlog:{organizationId}:{projectId}`, 2-minute TTL).

### Requirement 20: Department Board View (REQ-054)

**User Story:** As a DeptLead, I want a department board showing tasks grouped by department so that I can see workload distribution.

#### Acceptance Criteria

1. WHEN `GET /api/v1/boards/department` is called with optional `sprintId` and optional `projectId`, THE WorkService SHALL return tasks grouped by department, each department showing task count, member count, and tasks by status.
2. WHEN department board data is requested, THE WorkService SHALL cache results in Redis (`board_dept:{organizationId}:{projectId}:{sprintId}`, 2-minute TTL).

### Requirement 21: Comments on Stories and Tasks (REQ-055)

**User Story:** As a team member, I want to comment on stories and tasks so that I can collaborate with my team.

#### Acceptance Criteria

1. WHEN `POST /api/v1/comments` is called with `{entityType, entityId, content, parentCommentId?}`, THE WorkService SHALL create a comment, set `AuthorId` to the authenticated user, and return HTTP 201.
2. WHEN `entityType` is `Story` or `Task`, THE WorkService SHALL associate the comment with the corresponding entity.
3. WHEN `parentCommentId` is provided, THE WorkService SHALL create the comment as a reply (threaded comments).
4. WHEN `PUT /api/v1/comments/{id}` is called by the author, THE WorkService SHALL update the comment content and set `IsEdited` to `true`.
5. WHEN `PUT /api/v1/comments/{id}` is called by a non-author, THE WorkService SHALL return HTTP 403 with `COMMENT_NOT_AUTHOR` (4017).
6. WHEN `DELETE /api/v1/comments/{id}` is called by the author or OrgAdmin, THE WorkService SHALL soft-delete the comment (`FlgStatus=D`).
7. WHEN `GET /api/v1/stories/{id}/comments` or `GET /api/v1/tasks/{id}/comments` is called, THE WorkService SHALL return threaded comments sorted by creation date.
8. IF the comment does not exist, THEN THE WorkService SHALL return HTTP 404 with `COMMENT_NOT_FOUND` (4012).

### Requirement 22: @Mentions in Comments (REQ-056)

**User Story:** As a team member, I want to @mention colleagues in comments so that they are notified.

#### Acceptance Criteria

1. WHEN a comment contains `@{displayName}` or `@{email}`, THE WorkService SHALL resolve the mentioned user within the organization.
2. WHEN the mentioned user is found, THE WorkService SHALL publish a `MentionedInComment` notification to `outbox:work` with `MentionerName`, `StoryKey`, and `CommentPreview` (first 100 characters).
3. IF the mentioned user is not found in the organization, THEN THE WorkService SHALL return HTTP 400 with `MENTION_USER_NOT_FOUND` (4029).

### Requirement 23: Label Management (REQ-057)

**User Story:** As a team member, I want to create and apply labels to stories so that I can categorize and filter work.

#### Acceptance Criteria

1. WHEN `POST /api/v1/labels` is called with `{name, color}`, THE WorkService SHALL create an organization-scoped label and return HTTP 201.
2. WHEN the label name already exists in the organization, THE WorkService SHALL return HTTP 409 with `LABEL_NAME_DUPLICATE` (4011).
3. WHEN `POST /api/v1/stories/{id}/labels` is called with `{labelId}`, THE WorkService SHALL apply the label to the story.
4. WHEN a story already has 10 labels, THE WorkService SHALL return HTTP 400 with `MAX_LABELS_PER_STORY` (4040).
5. WHEN `DELETE /api/v1/stories/{id}/labels/{labelId}` is called, THE WorkService SHALL remove the label from the story.
6. WHEN `GET /api/v1/labels` is called, THE WorkService SHALL return all labels for the organization.
7. WHEN `PUT /api/v1/labels/{id}` is called, THE WorkService SHALL update the label name and color.
8. WHEN `DELETE /api/v1/labels/{id}` is called, THE WorkService SHALL delete the label.
9. IF the label does not exist, THEN THE WorkService SHALL return HTTP 404 with `LABEL_NOT_FOUND` (4010).

### Requirement 24: Activity Log — Story/Task Timeline (REQ-058)

**User Story:** As a team member, I want to see a timeline of all changes to a story or task so that I can understand the history.

#### Acceptance Criteria

1. WHEN a story or task is created, updated, status-changed, assigned, or commented on, THE WorkService SHALL create an `ActivityLog` entry with: `EntityType`, `EntityId`, `StoryKey`, `Action`, `ActorId`, `ActorName`, `OldValue`, `NewValue`, `Description`.
2. WHEN `GET /api/v1/stories/{id}/activity` or `GET /api/v1/tasks/{id}/activity` is called, THE WorkService SHALL return the activity timeline sorted by date descending.
3. THE WorkService SHALL track the following activity action types: `Created`, `StatusChanged`, `Assigned`, `Unassigned`, `PriorityChanged`, `PointsChanged`, `SprintAdded`, `SprintRemoved`, `LabelAdded`, `LabelRemoved`, `CommentAdded`, `DescriptionUpdated`, `DueDateChanged`, `TaskAdded`, `DepartmentChanged`.
4. THE WorkService SHALL scope all activity log entries to the organization via `OrganizationId`.

### Requirement 25: Full-Text Search (REQ-059)

**User Story:** As a team member, I want to search across stories and tasks so that I can quickly find relevant work items.

#### Acceptance Criteria

1. WHEN `GET /api/v1/search?q={query}` is called, THE WorkService SHALL perform full-text search across story titles, descriptions, acceptance criteria, story keys, task titles, and task descriptions using PostgreSQL `tsvector` and `tsquery`.
2. IF the query is less than 2 characters, THEN THE WorkService SHALL return HTTP 400 with `SEARCH_QUERY_TOO_SHORT` (4028).
3. WHEN search results are returned, THE WorkService SHALL include for each result: `EntityType` (Story/Task), `StoryKey`, `Title`, `Status`, `Priority`, `AssigneeName`, `DepartmentName`, and relevance score.
4. THE WorkService SHALL scope search results to the authenticated user's organization.
5. WHEN search supports filtering, THE WorkService SHALL accept optional filters: `status`, `priority`, `department`, `assignee`, `sprint`, `labels`, `entityType`, `dateRange`.
6. WHEN search results are requested, THE WorkService SHALL cache results in Redis (`search_results:{hash}`, 1-minute TTL).
7. THE WorkService SHALL use weighted full-text search: story keys and titles weighted `A` (highest), descriptions weighted `B`, with GIN indexes on the `search_vector` columns.

### Requirement 26: Saved Filters / Custom Views (REQ-060)

**User Story:** As a team member, I want to save frequently used filter combinations so that I can quickly access my preferred views.

#### Acceptance Criteria

1. WHEN `POST /api/v1/saved-filters` is called with `{name, filters}`, THE WorkService SHALL save the filter configuration for the authenticated user and return HTTP 201.
2. WHEN `GET /api/v1/saved-filters` is called, THE WorkService SHALL return all saved filters for the authenticated user.
3. WHEN `DELETE /api/v1/saved-filters/{id}` is called, THE WorkService SHALL remove the saved filter.
4. THE WorkService SHALL store saved filters as JSON in the `saved_filter` table, scoped to the organization and team member.

### Requirement 27: Reports — Sprint Velocity Chart (REQ-061)

**User Story:** As a project manager, I want a sprint velocity chart so that I can track team performance over time.

#### Acceptance Criteria

1. WHEN `GET /api/v1/reports/velocity` is called with optional `count` parameter, THE WorkService SHALL return velocity data for the last N completed sprints (default 10).
2. WHEN velocity data is returned, THE WorkService SHALL include for each entry: `SprintName`, `Velocity` (completed story points), `TotalStoryPoints`, `CompletionRate`, `StartDate`, `EndDate`.

### Requirement 28: Reports — Department Workload Distribution (REQ-062)

**User Story:** As an OrgAdmin, I want to see workload distribution across departments so that I can identify bottlenecks.

#### Acceptance Criteria

1. WHEN `GET /api/v1/reports/department-workload` is called with optional `sprintId`, THE WorkService SHALL return per-department metrics: `DepartmentName`, `TotalTasks`, `CompletedTasks`, `InProgressTasks`, `MemberCount`, `AvgTasksPerMember`.
2. WHEN a `sprintId` is provided, THE WorkService SHALL scope metrics to that sprint. WHEN omitted, THE WorkService SHALL cover all active tasks.

### Requirement 29: Reports — Team Member Capacity Utilization (REQ-063)

**User Story:** As a DeptLead, I want to see team member capacity utilization so that I can balance workload.

#### Acceptance Criteria

1. WHEN `GET /api/v1/reports/capacity` is called with optional `departmentId`, THE WorkService SHALL return per-member metrics: `MemberName`, `Department`, `ActiveTasks`, `MaxConcurrentTasks`, `UtilizationRate` (`ActiveTasks / MaxConcurrentTasks * 100`), `Availability`.
2. WHEN `departmentId` is provided, THE WorkService SHALL filter results to that department.

### Requirement 30: Reports — Story Cycle Time (REQ-064)

**User Story:** As a project manager, I want to track story cycle time so that I can identify process improvements.

#### Acceptance Criteria

1. WHEN `GET /api/v1/reports/cycle-time` is called with optional date range, THE WorkService SHALL return cycle time data for completed stories: `StoryKey`, `Title`, `CycleTimeDays` (InProgress → Done), `LeadTimeDays` (Created → Done), `CompletedDate`.
2. WHEN cycle time is calculated, THE WorkService SHALL measure the time from when a story entered `InProgress` to when it reached `Done`.

### Requirement 31: Reports — Task Completion Rate by Department (REQ-065)

**User Story:** As an OrgAdmin, I want to see task completion rates by department so that I can assess team effectiveness.

#### Acceptance Criteria

1. WHEN `GET /api/v1/reports/task-completion` is called with optional date range and `sprintId`, THE WorkService SHALL return per-department: `DepartmentName`, `TotalTasks`, `CompletedTasks`, `CompletionRate`, `AvgCompletionTimeHours`.

### Requirement 32: Workflow Customization (REQ-066)

**User Story:** As an OrgAdmin, I want to customize workflows so that the platform adapts to our specific process.

#### Acceptance Criteria

1. WHEN `GET /api/v1/workflows` is called, THE WorkService SHALL return the default workflow definitions for stories and tasks.
2. WHEN `PUT /api/v1/workflows/organization` is called by an OrgAdmin, THE WorkService SHALL save organization-level workflow overrides (custom status names, additional statuses, modified transitions).
3. WHEN `PUT /api/v1/workflows/department/{departmentId}` is called by an OrgAdmin or DeptLead, THE WorkService SHALL save department-level workflow overrides.
4. WHEN workflow transitions are validated, THE WorkService SHALL check organization-level overrides first, then fall back to default workflows.

### Requirement 33: Real-Time Board Updates (REQ-067)

**User Story:** As a team member, I want the board to update in near-real-time so that I see changes as they happen.

#### Acceptance Criteria

1. WHEN a story or task status changes, THE WorkService SHALL invalidate the relevant board caches (`board_kanban:*`, `board_dept:*`, `board_backlog:*`, `sprint_metrics:*`) for the affected project and organization.
2. WHEN the frontend polls for board updates, THE WorkService SHALL ensure near-real-time data via short cache TTLs (2 minutes for boards, 5 minutes for metrics).
3. WHEN a story or task is created, updated, assigned, or deleted, THE WorkService SHALL invalidate the `story_detail:{storyId}` cache.

### Requirement 34: WorkService API Endpoints (REQ-068)

**User Story:** As a developer, I want a complete set of work management endpoints so that all Agile workflow operations are supported.

#### Acceptance Criteria

1. THE WorkService SHALL expose the following endpoints:

**Project Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/projects` | Bearer (OrgAdmin, DeptLead) | Create project |
| GET | `/api/v1/projects` | Bearer | List projects |
| GET | `/api/v1/projects/{id}` | Bearer | Get project detail |
| PUT | `/api/v1/projects/{id}` | Bearer (OrgAdmin, DeptLead) | Update project |
| PATCH | `/api/v1/projects/{id}/status` | Bearer (OrgAdmin) | Update project status |

**Story Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/stories` | Bearer (OrgAdmin, DeptLead, Member) | Create story |
| GET | `/api/v1/stories` | Bearer | List stories (paginated, filtered) |
| GET | `/api/v1/stories/{id}` | Bearer | Get story detail |
| GET | `/api/v1/stories/by-key/{storyKey}` | Bearer | Get story by key |
| PUT | `/api/v1/stories/{id}` | Bearer (OrgAdmin, DeptLead, Member) | Update story |
| DELETE | `/api/v1/stories/{id}` | Bearer (OrgAdmin, DeptLead) | Soft delete story |
| PATCH | `/api/v1/stories/{id}/status` | Bearer (Member+) | Transition story status |
| PATCH | `/api/v1/stories/{id}/assign` | Bearer (DeptLead+) | Assign story to member |
| PATCH | `/api/v1/stories/{id}/unassign` | Bearer (DeptLead+) | Unassign story |
| POST | `/api/v1/stories/{id}/links` | Bearer (Member+) | Create story link |
| DELETE | `/api/v1/stories/{id}/links/{linkId}` | Bearer (Member+) | Remove story link |
| POST | `/api/v1/stories/{id}/labels` | Bearer (Member+) | Apply label to story |
| DELETE | `/api/v1/stories/{id}/labels/{labelId}` | Bearer (Member+) | Remove label from story |
| GET | `/api/v1/stories/{id}/comments` | Bearer | List story comments |
| GET | `/api/v1/stories/{id}/activity` | Bearer | Get story activity log |

**Task Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/tasks` | Bearer (OrgAdmin, DeptLead, Member) | Create task |
| GET | `/api/v1/tasks/{id}` | Bearer | Get task detail |
| GET | `/api/v1/stories/{storyId}/tasks` | Bearer | List tasks for story |
| PUT | `/api/v1/tasks/{id}` | Bearer (OrgAdmin, DeptLead, Member) | Update task |
| DELETE | `/api/v1/tasks/{id}` | Bearer (OrgAdmin, DeptLead) | Soft delete task |
| PATCH | `/api/v1/tasks/{id}/status` | Bearer (Member+) | Transition task status |
| PATCH | `/api/v1/tasks/{id}/assign` | Bearer (DeptLead+) | Assign task to member |
| PATCH | `/api/v1/tasks/{id}/self-assign` | Bearer (Member+) | Self-assign task |
| PATCH | `/api/v1/tasks/{id}/unassign` | Bearer (DeptLead+) | Unassign task |
| PATCH | `/api/v1/tasks/{id}/log-hours` | Bearer (Member+) | Log actual hours |
| DELETE | `/api/v1/tasks/{id}` | Bearer (DeptLead+) | Soft-delete task |
| GET | `/api/v1/tasks/{id}/activity` | Bearer | Get task activity feed |
| GET | `/api/v1/tasks/{id}/comments` | Bearer | List task comments |
| POST | `/api/v1/tasks/{id}/comments` | Bearer (Member+) | Add task comment |
| GET | `/api/v1/tasks/suggest-assignee` | Bearer | Get auto-assignment suggestion |

**Sprint Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/projects/{projectId}/sprints` | Bearer (OrgAdmin, DeptLead) | Create sprint |
| GET | `/api/v1/sprints` | Bearer | List sprints (filterable by projectId) |
| GET | `/api/v1/sprints/{id}` | Bearer | Get sprint detail |
| PUT | `/api/v1/sprints/{id}` | Bearer (OrgAdmin, DeptLead) | Update sprint |
| PATCH | `/api/v1/sprints/{id}/start` | Bearer (OrgAdmin, DeptLead) | Start sprint |
| PATCH | `/api/v1/sprints/{id}/complete` | Bearer (OrgAdmin, DeptLead) | Complete sprint |
| PATCH | `/api/v1/sprints/{id}/cancel` | Bearer (OrgAdmin, DeptLead) | Cancel sprint |
| POST | `/api/v1/sprints/{sprintId}/stories` | Bearer (OrgAdmin, DeptLead) | Add story to sprint |
| DELETE | `/api/v1/sprints/{sprintId}/stories/{storyId}` | Bearer (OrgAdmin, DeptLead) | Remove story from sprint |
| GET | `/api/v1/sprints/{id}/metrics` | Bearer | Get sprint metrics |
| GET | `/api/v1/sprints/velocity` | Bearer | Get velocity history |
| GET | `/api/v1/sprints/active` | Bearer | Get current active sprint |

**Board & View Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/v1/boards/kanban` | Bearer | Kanban board view (optional projectId filter) |
| GET | `/api/v1/boards/sprint` | Bearer | Sprint board view (optional projectId filter) |
| GET | `/api/v1/boards/backlog` | Bearer | Backlog view (optional projectId filter) |
| GET | `/api/v1/boards/department` | Bearer | Department board view (optional projectId filter) |

**Comment Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/comments` | Bearer (Member+) | Create comment |
| PUT | `/api/v1/comments/{id}` | Bearer (Author) | Edit comment |
| DELETE | `/api/v1/comments/{id}` | Bearer (Author, OrgAdmin) | Delete comment |

**Label Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/labels` | Bearer (DeptLead+) | Create label |
| GET | `/api/v1/labels` | Bearer | List labels |
| PUT | `/api/v1/labels/{id}` | Bearer (DeptLead+) | Update label |
| DELETE | `/api/v1/labels/{id}` | Bearer (OrgAdmin) | Delete label |

**Search Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/v1/search` | Bearer | Full-text search |

**Saved Filter Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/saved-filters` | Bearer | Save filter |
| GET | `/api/v1/saved-filters` | Bearer | List saved filters |
| DELETE | `/api/v1/saved-filters/{id}` | Bearer | Delete saved filter |

**Report Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/v1/reports/velocity` | Bearer | Velocity chart data |
| GET | `/api/v1/reports/department-workload` | Bearer | Department workload |
| GET | `/api/v1/reports/capacity` | Bearer | Capacity utilization |
| GET | `/api/v1/reports/cycle-time` | Bearer | Story cycle time |
| GET | `/api/v1/reports/task-completion` | Bearer | Task completion rate |

**Workflow Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/v1/workflows` | Bearer | Get workflow definitions |
| PUT | `/api/v1/workflows/organization` | OrgAdmin | Override org workflows |
| PUT | `/api/v1/workflows/department/{deptId}` | OrgAdmin, DeptLead | Override dept workflows |

**System Endpoints:**

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/health` | None | Health check |
| GET | `/ready` | None | Readiness check |

2. THE WorkService SHALL use URL path versioning with prefix `/api/v1/`.
3. THE WorkService SHALL return all responses in the `ApiResponse<T>` envelope format with `CorrelationId`.
4. WHEN a request fails FluentValidation, THE WorkService SHALL return HTTP 422 with error code `VALIDATION_ERROR` (1000) and the list of validation errors.

### Requirement 35: WorkService Redis Key Patterns (REQ-069)

**User Story:** As a developer, I want well-defined Redis key patterns so that caching, board views, and metrics are consistent and predictable.

#### Acceptance Criteria

1. THE WorkService SHALL use the following Redis key patterns with their specified TTLs:

| Pattern | Purpose | TTL |
|---------|---------|-----|
| `project_prefix:{projectId}` | Cached project story key prefix | 60 min |
| `sprint_active:{projectId}` | Cached active sprint ID for a project | 5 min |
| `board_kanban:{organizationId}:{projectId}:{sprintId}` | Cached kanban board data | 2 min |
| `board_backlog:{organizationId}:{projectId}` | Cached backlog data | 2 min |
| `board_dept:{organizationId}:{projectId}:{sprintId}` | Cached department board data | 2 min |
| `sprint_metrics:{sprintId}` | Cached sprint metrics | 5 min |
| `story_detail:{storyId}` | Cached story detail | 5 min |
| `search_results:{hash}` | Cached search results by query hash | 1 min |
| `outbox:work` | Outbox queue for audit events and notifications | Until processed |
| `blacklist:{jti}` | Token deny list (shared) | Remaining token TTL |

2. THE WorkService SHALL use consistent key naming with colon-separated segments.
3. THE WorkService SHALL set appropriate TTLs on all Redis keys to prevent unbounded memory growth.

### Requirement 36: WorkService Data Models (REQ-070)

**User Story:** As a developer, I want well-defined data models so that the WorkService database schema is clear and supports all Agile workflow operations.

#### Acceptance Criteria

1. THE WorkService SHALL maintain a `project` table with columns: `ProjectId` (Guid PK), `OrganizationId` (Guid FK), `ProjectName` (string, required, max 200), `ProjectKey` (string, required, 2-10 uppercase alphanumeric, unique globally), `Description` (string?), `LeadId` (Guid?), `FlgStatus` (string, default `A`), `DateCreated` (DateTime), `DateUpdated` (DateTime). Unique index on `ProjectKey` (global) and `(OrganizationId, ProjectName)`.
2. THE WorkService SHALL maintain a `story` table with columns: `StoryId` (Guid PK), `OrganizationId` (Guid FK), `ProjectId` (Guid FK), `StoryKey` (string, unique per project), `SequenceNumber` (long), `Title` (string, max 200), `Description` (string, max 5000), `AcceptanceCriteria` (string, max 5000), `StoryPoints` (int?), `Priority` (string), `Status` (string), `AssigneeId` (Guid?), `ReporterId` (Guid), `SprintId` (Guid?), `DepartmentId` (Guid?), `DueDate` (DateTime?), `CompletedDate` (DateTime?), `FlgStatus` (string), `DateCreated` (DateTime), `DateUpdated` (DateTime).
3. THE WorkService SHALL maintain a `task` table with columns: `TaskId` (Guid PK), `OrganizationId` (Guid FK), `StoryId` (Guid FK), `Title` (string, max 200), `Description` (string, max 3000), `TaskType` (string), `Status` (string), `Priority` (string), `AssigneeId` (Guid?), `DepartmentId` (Guid?), `EstimatedHours` (decimal?), `ActualHours` (decimal?), `DueDate` (DateTime?), `CompletedDate` (DateTime?), `FlgStatus` (string), `DateCreated` (DateTime), `DateUpdated` (DateTime).
4. THE WorkService SHALL maintain a `sprint` table with columns: `SprintId` (Guid PK), `OrganizationId` (Guid FK), `ProjectId` (Guid FK), `SprintName` (string, max 100), `Goal` (string, max 500), `StartDate` (DateTime), `EndDate` (DateTime), `Status` (string), `Velocity` (int?), `DateCreated` (DateTime), `DateUpdated` (DateTime).
4. THE WorkService SHALL maintain a `sprint` table with columns: `SprintId` (Guid PK), `OrganizationId` (Guid FK), `ProjectId` (Guid FK), `SprintName` (string, max 100), `Goal` (string, max 500), `StartDate` (DateTime), `EndDate` (DateTime), `Status` (string), `Velocity` (int?), `DateCreated` (DateTime), `DateUpdated` (DateTime).
5. THE WorkService SHALL maintain a `sprint_story` table with columns: `SprintStoryId` (Guid PK), `SprintId` (Guid FK), `StoryId` (Guid FK), `AddedDate` (DateTime), `RemovedDate` (DateTime?). Unique index on `(SprintId, StoryId)` where `RemovedDate IS NULL`.
6. THE WorkService SHALL maintain a `comment` table with columns: `CommentId` (Guid PK), `OrganizationId` (Guid FK), `EntityType` (string), `EntityId` (Guid), `AuthorId` (Guid FK), `Content` (string), `ParentCommentId` (Guid?), `IsEdited` (bool), `FlgStatus` (string), `DateCreated` (DateTime), `DateUpdated` (DateTime).
7. THE WorkService SHALL maintain an `activity_log` table with columns: `ActivityLogId` (Guid PK), `OrganizationId` (Guid FK), `EntityType` (string), `EntityId` (Guid), `StoryKey` (string), `Action` (string), `ActorId` (Guid), `ActorName` (string), `OldValue` (string?), `NewValue` (string?), `Description` (string), `DateCreated` (DateTime).
8. THE WorkService SHALL maintain a `label` table with columns: `LabelId` (Guid PK), `OrganizationId` (Guid FK), `Name` (string, max 50), `Color` (string, max 7), `DateCreated` (DateTime). Unique index on `(OrganizationId, Name)`.
9. THE WorkService SHALL maintain a `story_label` table with columns: `StoryLabelId` (Guid PK), `StoryId` (Guid FK), `LabelId` (Guid FK). Unique index on `(StoryId, LabelId)`.
10. THE WorkService SHALL maintain a `story_link` table with columns: `StoryLinkId` (Guid PK), `OrganizationId` (Guid FK), `SourceStoryId` (Guid FK), `TargetStoryId` (Guid FK), `LinkType` (string), `DateCreated` (DateTime).
11. THE WorkService SHALL maintain a `story_sequence` table with columns: `ProjectId` (Guid PK), `CurrentValue` (long, default 0).
12. THE WorkService SHALL maintain a `saved_filter` table with columns: `SavedFilterId` (Guid PK), `OrganizationId` (Guid FK), `TeamMemberId` (Guid FK), `Name` (string), `Filters` (string — JSON), `DateCreated` (DateTime).
13. THE WorkService SHALL use EF Core with PostgreSQL (Npgsql) and apply auto-migrations via `DatabaseMigrationHelper` on startup.
14. THE WorkService SHALL add PostgreSQL full-text search `tsvector` columns on `story` and `task` tables with GIN indexes for search performance.

### Requirement 37: Clean Architecture Layer Structure (REQ-086)

**User Story:** As a developer, I want WorkService to follow Clean Architecture so that the codebase is maintainable and testable.

#### Acceptance Criteria

1. THE WorkService SHALL be structured as four projects: `WorkService.Domain`, `WorkService.Application`, `WorkService.Infrastructure`, `WorkService.Api`.
2. WHEN the Domain layer is built, THE WorkService SHALL have zero `ProjectReference` entries and zero ASP.NET Core or EF Core package references.
3. WHEN the Application layer is built, THE WorkService SHALL reference only `WorkService.Domain` and contain no infrastructure packages.
4. WHEN the Infrastructure layer is built, THE WorkService SHALL reference `WorkService.Domain` and `WorkService.Application`.
5. WHEN the Api layer is built, THE WorkService SHALL reference `WorkService.Application` and `WorkService.Infrastructure` and serve as the composition root.

### Requirement 38: Organization Isolation — Global Query Filters (REQ-087)

**User Story:** As the platform, I want all WorkService database queries to be automatically scoped to the current organization so that data isolation is enforced at the database level.

#### Acceptance Criteria

1. WHEN EF Core queries are executed, THE WorkService SHALL apply global query filters that automatically scope all queries by `OrganizationId`.
2. WHEN an entity implements `IOrganizationEntity`, THE WorkService SHALL apply the global query filter.
3. WHEN `OrganizationScopeMiddleware` processes a request, THE WorkService SHALL extract `organizationId` from JWT claims and store it in `HttpContext.Items["OrganizationId"]`.
4. WHEN inter-service calls are made, THE WorkService SHALL propagate the `X-Organization-Id` header via `CorrelationIdDelegatingHandler`.

### Requirement 39: Standardized Error Handling (REQ-088)

**User Story:** As a developer, I want WorkService to handle errors consistently so that clients receive predictable error responses.

#### Acceptance Criteria

1. WHEN a `DomainException` is thrown, THE WorkService SHALL catch it via `GlobalExceptionHandlerMiddleware` and return an `ApiResponse<object>` with `application/problem+json` content type, including the error's `ErrorCode`, `ErrorValue`, `Message`, and `CorrelationId`.
2. WHEN an unhandled exception is thrown, THE WorkService SHALL return HTTP 500 with `ErrorCode = "INTERNAL_ERROR"`, `Message = "An unexpected error occurred."`, and `CorrelationId`. THE WorkService SHALL not leak stack traces or internal details.
3. WHEN a `RateLimitExceededException` is thrown, THE WorkService SHALL add a `Retry-After` header to the error response.
4. WHEN any error response is returned, THE WorkService SHALL include the `CorrelationId` from `HttpContext.Items["CorrelationId"]`.

### Requirement 40: ApiResponse Envelope (REQ-089)

**User Story:** As a developer, I want all WorkService API responses wrapped in a standardized envelope so that clients can parse responses consistently.

#### Acceptance Criteria

1. WHEN any endpoint returns a response, THE WorkService SHALL wrap it in `ApiResponse<T>` with fields: `ResponseCode`, `ResponseDescription`, `Success`, `Data`, `ErrorCode`, `ErrorValue`, `Message`, `CorrelationId`, `Errors`.
2. WHEN a successful response is returned, THE WorkService SHALL set `ResponseCode = "00"`, `Success = true`, and `Data` containing the payload.
3. WHEN a validation error occurs, THE WorkService SHALL set `ResponseCode = "96"`, `ErrorCode = "VALIDATION_ERROR"`, and `Errors` containing per-field error details.

### Requirement 41: FluentValidation Pipeline (REQ-090)

**User Story:** As a developer, I want automatic request validation so that invalid data is rejected before reaching business logic.

#### Acceptance Criteria

1. WHEN a request DTO has a corresponding FluentValidation validator, THE WorkService SHALL auto-discover and execute it before the controller action.
2. WHEN validation fails, THE WorkService SHALL return HTTP 422 with `ErrorCode = "VALIDATION_ERROR"`, `ErrorValue = 1000`, and per-field errors in the `Errors` array as `{ field, message }` objects.
3. THE WorkService SHALL disable ASP.NET Core's built-in `ModelStateInvalidFilter` via `SuppressModelStateInvalidFilter = true` to let FluentValidation handle all validation.

### Requirement 42: Inter-Service Resilience — Typed Service Clients (REQ-091)

**User Story:** As a developer, I want typed service clients with Polly resilience policies so that inter-service communication from WorkService is reliable and fault-tolerant.

#### Acceptance Criteria

1. WHEN WorkService communicates with ProfileService, THE WorkService SHALL use a typed service client interface (`IProfileServiceClient`).
2. WHEN WorkService communicates with SecurityService, THE WorkService SHALL use a typed service client interface (`ISecurityServiceClient`).
3. WHEN the typed client makes an HTTP call, THE WorkService SHALL apply Polly resilience policies: 3 retries with exponential backoff (1s, 2s, 4s), circuit breaker (5 failures → 30s open), and 10s timeout per request.
4. WHEN a downstream service returns 4xx or 5xx, THE WorkService SHALL attempt to deserialize the response as `ApiResponse<object>` and throw a `DomainException` with the downstream error code. IF deserialization fails, THEN THE WorkService SHALL throw a `DomainException` with `SERVICE_UNAVAILABLE` (4038).
5. WHEN the circuit breaker opens, THE WorkService SHALL throw a `DomainException` with `SERVICE_UNAVAILABLE` (4038).
6. WHEN an inter-service call is made, THE WorkService SHALL propagate the `X-Correlation-Id` header via `CorrelationIdDelegatingHandler`.
7. WHEN a downstream call fails, THE WorkService SHALL log at Warning level with structured properties: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`.

### Requirement 43: CorrelationId Propagation (REQ-092)

**User Story:** As a developer, I want end-to-end request tracing so that I can debug issues across services.

#### Acceptance Criteria

1. WHEN a request enters WorkService, THE WorkService SHALL extract `X-Correlation-Id` from the request header via `CorrelationIdMiddleware` or generate a new GUID if absent.
2. WHEN the correlation ID is established, THE WorkService SHALL store it in `HttpContext.Items["CorrelationId"]` and add it to the response header.
3. WHEN an inter-service call is made, THE WorkService SHALL attach the `X-Correlation-Id` header to the outgoing request via `CorrelationIdDelegatingHandler`.
4. WHEN any error response is returned, THE WorkService SHALL include the `CorrelationId` in the `ApiResponse` body.

### Requirement 44: Redis Outbox Pattern (REQ-093)

**User Story:** As the platform, I want async event processing via Redis outbox so that audit logging and notifications from WorkService do not block API responses.

#### Acceptance Criteria

1. WHEN WorkService needs to publish an audit event or notification, THE WorkService SHALL call `IOutboxService.PublishAsync("outbox:work", serializedMessage)` which pushes to the WorkService Redis outbox queue.
2. THE WorkService SHALL publish outbox messages in the standardized format with `Type` ("notification" or "audit") and `Payload` containing the event data.
3. WHEN a story is created, updated, assigned, or status-changed, THE WorkService SHALL publish an audit event to `outbox:work`.
4. WHEN a notification-triggering event occurs (StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged), THE WorkService SHALL publish a notification event to `outbox:work`.

### Requirement 45: Database Migrations — Auto-Apply (REQ-094)

**User Story:** As a developer, I want database migrations to auto-apply on startup so that deployment is simplified.

#### Acceptance Criteria

1. WHEN WorkService starts, THE WorkService SHALL call `DatabaseMigrationHelper.ApplyMigrations(app)` to check for pending EF Core migrations and apply them.
2. WHEN the database is InMemory (test environment), THE WorkService SHALL call `EnsureCreated()` instead of `Migrate()`.
3. WHEN no pending migrations exist, THE WorkService SHALL proceed with startup without database changes.

### Requirement 46: Health Checks (REQ-095)

**User Story:** As a DevOps engineer, I want health check endpoints so that I can monitor WorkService availability.

#### Acceptance Criteria

1. WHEN `GET /health` is called, THE WorkService SHALL return HTTP 200 if the process is running (liveness probe).
2. WHEN `GET /ready` is called, THE WorkService SHALL check database connectivity and Redis connection and return HTTP 200 if both are healthy (readiness probe).

### Requirement 47: Pagination (REQ-096)

**User Story:** As a developer, I want consistent pagination across all WorkService list endpoints so that large datasets are handled efficiently.

#### Acceptance Criteria

1. WHEN any list endpoint is called, THE WorkService SHALL support `page` (default 1) and `pageSize` (default 20, max 100) query parameters.
2. WHEN the response is paginated, THE WorkService SHALL include: `TotalCount`, `Page`, `PageSize`, `TotalPages`, and the `Data` array.
3. WHEN `pageSize` exceeds 100, THE WorkService SHALL cap it at 100.

### Requirement 48: Soft Delete Pattern (REQ-097)

**User Story:** As the platform, I want soft deletes so that WorkService data is never permanently lost and can be recovered if needed.

#### Acceptance Criteria

1. WHEN an entity is "deleted", THE WorkService SHALL set its `FlgStatus` to `D` instead of physically removing it.
2. WHEN entities are queried, THE WorkService SHALL apply EF Core global query filters to exclude entities with `FlgStatus = 'D'` by default.
3. WHEN an admin needs to see deleted entities, THE WorkService SHALL support bypassing the query filter with `.IgnoreQueryFilters()`.

### Requirement 49: Structured Logging (REQ-098)

**User Story:** As a developer, I want structured logging so that WorkService logs are searchable and correlatable.

#### Acceptance Criteria

1. WHEN a `DomainException` is logged, THE WorkService SHALL include: `CorrelationId`, `ErrorCode`, `ErrorValue`, `ServiceName` ("WorkService"), `RequestPath`.
2. WHEN an unhandled exception is logged, THE WorkService SHALL include: `CorrelationId`, `ServiceName`, `RequestPath`, `ExceptionType`.
3. WHEN a downstream call fails, THE WorkService SHALL include: `CorrelationId`, `DownstreamService`, `DownstreamEndpoint`, `HttpStatusCode`, `ElapsedMs`.

### Requirement 50: Service-to-Service JWT Token Management (REQ-099)

**User Story:** As a developer, I want automatic service JWT management in WorkService typed clients so that inter-service auth is seamless.

#### Acceptance Criteria

1. WHEN a typed service client makes a call, THE WorkService SHALL automatically attach a service JWT via `Authorization: Bearer {token}`.
2. WHEN the cached service token is within 30 seconds of expiry, THE WorkService SHALL automatically refresh it by calling SecurityService `POST /api/v1/service-tokens/issue`.
3. WHEN the `X-Organization-Id` header is available in the current request context, THE WorkService SHALL propagate it to the downstream call.

### Requirement 51: API Versioning (REQ-100)

**User Story:** As a developer, I want API versioning so that breaking changes can be introduced without affecting existing clients.

#### Acceptance Criteria

1. THE WorkService SHALL use URL path versioning: `/api/v1/...` for all endpoints.
2. WHEN a new version is needed, THE WorkService SHALL add it as `/api/v2/...` without removing the v1 endpoints.

### Requirement 52: Configuration via Environment Variables (REQ-101)

**User Story:** As a DevOps engineer, I want all WorkService configuration via environment variables so that the service is 12-factor compliant.

#### Acceptance Criteria

1. WHEN WorkService starts, THE WorkService SHALL load configuration from a `.env` file via `DotNetEnv` and populate an `AppSettings` singleton.
2. IF a required environment variable is missing, THEN THE WorkService SHALL throw `InvalidOperationException` at startup with a clear message.
3. WHEN optional environment variables are missing, THE WorkService SHALL use sensible defaults.

### Requirement 53: CORS Configuration (REQ-102)

**User Story:** As a developer, I want CORS configured so that the frontend can communicate with WorkService.

#### Acceptance Criteria

1. WHEN WorkService starts, THE WorkService SHALL configure CORS with allowed origins from the `ALLOWED_ORIGINS` environment variable (comma-separated).
2. WHEN a preflight request is received, THE WorkService SHALL respond with appropriate CORS headers.

### Requirement 54: Swagger Documentation (REQ-103)

**User Story:** As a developer, I want Swagger UI so that I can explore and test WorkService API endpoints.

#### Acceptance Criteria

1. WHEN WorkService is running in Development mode, THE WorkService SHALL make Swagger UI available at `http://localhost:5003/swagger`.
2. WHEN Swagger is configured, THE WorkService SHALL include JWT Bearer authentication support for testing authenticated endpoints.

### Requirement 55: Inter-Service Communication Map (REQ-105)

**User Story:** As a developer, I want a clear map of WorkService inter-service dependencies so that I understand the communication topology.

#### Acceptance Criteria

1. THE WorkService SHALL communicate with the following services:

| Callee | Purpose |
|--------|---------|
| ProfileService | Organization settings (`GET /api/v1/organizations/{id}/settings`) for sprint duration defaults, team member lookup, department member lists (`GET /api/v1/departments/{id}/members`), department by code lookup |
| SecurityService | Service token issuance (`POST /api/v1/service-tokens/issue`) |
| UtilityService | Via outbox (`outbox:work`) for audit events and notifications |

2. THE WorkService SHALL use typed service client interfaces (`IProfileServiceClient`, `ISecurityServiceClient`) with Polly resilience policies for all synchronous inter-service calls.
3. THE WorkService SHALL use the Redis outbox pattern for all asynchronous communication with UtilityService.

### Requirement 56: WorkService Error Codes (4001–4046)

**User Story:** As a developer, I want well-defined error codes so that WorkService error responses are consistent and machine-parseable.

#### Acceptance Criteria

1. THE WorkService SHALL define the following error codes in the `ErrorCodes` static class in the Domain layer:

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| VALIDATION_ERROR | 1000 | 422 | FluentValidation pipeline failure |
| STORY_NOT_FOUND | 4001 | 404 | Story does not exist |
| TASK_NOT_FOUND | 4002 | 404 | Task does not exist |
| SPRINT_NOT_FOUND | 4003 | 404 | Sprint does not exist |
| INVALID_STORY_TRANSITION | 4004 | 400 | Invalid story workflow state transition |
| INVALID_TASK_TRANSITION | 4005 | 400 | Invalid task workflow state transition |
| SPRINT_NOT_IN_PLANNING | 4006 | 400 | Stories can only be added to sprints in Planning status |
| STORY_ALREADY_IN_SPRINT | 4007 | 409 | Story already assigned to this sprint |
| STORY_NOT_IN_SPRINT | 4008 | 400 | Story not in this sprint |
| SPRINT_OVERLAP | 4009 | 400 | Sprint dates overlap with existing sprint |
| LABEL_NOT_FOUND | 4010 | 404 | Label does not exist |
| LABEL_NAME_DUPLICATE | 4011 | 409 | Duplicate label name in organization |
| COMMENT_NOT_FOUND | 4012 | 404 | Comment does not exist |
| STORY_REQUIRES_ASSIGNEE | 4013 | 400 | Story must have assignee for InProgress transition |
| STORY_REQUIRES_TASKS | 4014 | 400 | Story must have tasks for InReview transition |
| STORY_REQUIRES_POINTS | 4015 | 400 | Story must have story points for Ready transition |
| ONLY_ONE_ACTIVE_SPRINT | 4016 | 400 | Only one sprint can be active per project |
| COMMENT_NOT_AUTHOR | 4017 | 403 | Only author can edit/delete comment |
| ASSIGNEE_NOT_IN_DEPARTMENT | 4018 | 400 | Assignee not a member of target department |
| ASSIGNEE_AT_CAPACITY | 4019 | 400 | Assignee has reached max concurrent tasks |
| STORY_KEY_NOT_FOUND | 4020 | 404 | Story key does not resolve |
| SPRINT_ALREADY_ACTIVE | 4021 | 400 | Sprint is already active |
| SPRINT_ALREADY_COMPLETED | 4022 | 400 | Sprint is already completed |
| INVALID_STORY_POINTS | 4023 | 400 | Story points must be Fibonacci (1,2,3,5,8,13,21) |
| INVALID_PRIORITY | 4024 | 400 | Unknown priority value |
| INVALID_TASK_TYPE | 4025 | 400 | Unknown task type |
| STORY_IN_ACTIVE_SPRINT | 4026 | 400 | Cannot delete story in active sprint |
| TASK_IN_PROGRESS | 4027 | 400 | Cannot delete task that is in progress |
| SEARCH_QUERY_TOO_SHORT | 4028 | 400 | Search query must be at least 2 characters |
| MENTION_USER_NOT_FOUND | 4029 | 400 | @mentioned user not found in organization |
| ORGANIZATION_MISMATCH | 4030 | 403 | Cross-organization access |
| DEPARTMENT_ACCESS_DENIED | 4031 | 403 | User not in target department |
| INSUFFICIENT_PERMISSIONS | 4032 | 403 | Role lacks access for this operation |
| SPRINT_END_BEFORE_START | 4033 | 400 | End date must be after start date |
| STORY_SEQUENCE_INIT_FAILED | 4034 | 500 | Failed to initialize story sequence |
| HOURS_MUST_BE_POSITIVE | 4035 | 400 | Logged hours must be > 0 |
| NOT_FOUND | 4036 | 404 | Generic entity not found |
| CONFLICT | 4037 | 409 | Duplicate or state conflict |
| SERVICE_UNAVAILABLE | 4038 | 503 | Downstream timeout or circuit open |
| STORY_DESCRIPTION_REQUIRED | 4039 | 400 | Description required for Ready transition |
| MAX_LABELS_PER_STORY | 4040 | 400 | Maximum 10 labels per story |
| PROJECT_NOT_FOUND | 4041 | 404 | Project does not exist |
| PROJECT_NAME_DUPLICATE | 4042 | 409 | Duplicate project name in organization |
| PROJECT_KEY_DUPLICATE | 4043 | 409 | Duplicate project key (globally unique) |
| PROJECT_KEY_IMMUTABLE | 4044 | 400 | Cannot change project key after stories exist |
| PROJECT_KEY_INVALID_FORMAT | 4045 | 400 | Project key must be 2–10 uppercase alphanumeric |
| STORY_PROJECT_MISMATCH | 4046 | 400 | Story does not belong to the sprint's project |

2. THE WorkService SHALL use the multi-tier error code resolution chain: in-memory `ConcurrentDictionary` → Redis hash `error_codes_registry` (24h TTL) → HTTP call to UtilityService `GET /api/v1/error-codes` → local static fallback map.

### Requirement 57: WorkService Middleware Pipeline Order

**User Story:** As the platform, I want a well-defined middleware pipeline so that security and cross-cutting concerns are enforced in the correct order.

#### Acceptance Criteria

1. WHEN a request enters WorkService, THE WorkService SHALL execute middleware in this exact order: CORS → CorrelationId → GlobalExceptionHandler → RateLimiter → Routing → Authentication → Authorization → JwtClaims → TokenBlacklist → RoleAuthorization → OrganizationScope → Controllers.
2. WHEN `GlobalExceptionHandlerMiddleware` catches a `DomainException`, THE WorkService SHALL return the appropriate HTTP status code with `application/problem+json` content type and the `ApiResponse<T>` envelope including `ErrorCode`, `ErrorValue`, and `CorrelationId`.
3. WHEN `GlobalExceptionHandlerMiddleware` catches an unhandled exception, THE WorkService SHALL return HTTP 500 with a generic error message and publish an error event to `outbox:work`.
4. THE WorkService SHALL generate or propagate a `CorrelationId` (`X-Correlation-Id` header) on every request via `CorrelationIdMiddleware` and include it in all API responses.
