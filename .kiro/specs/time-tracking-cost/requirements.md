# Requirements Document — Time Tracking & Cost Data

## Introduction

This feature adds time tracking, cost rate management, and time policy capabilities to the Nexus 2.0 WorkService. Team members can log time entries against stories (via start/stop timer or manual entry), organizations can define cost rates at member, role, and department levels, and org-level time policies govern required hours, overtime rules, and approval workflows. The data feeds into project cost calculations, sprint velocity enrichment, and resource utilization metrics — forming the foundation for a larger analytics/reporting system.

All new entities are organization-scoped (`IOrganizationEntity`), soft-deletable (`FlgStatus`), and follow the established subfolder convention for Domain interfaces, Infrastructure repositories, and services.

## Glossary

- **Time_Entry**: A record of work performed by a team member against a story. Contains duration, start/end timestamps, billable flag, and optional notes. Supports both timer-based (start/stop) and manual entry modes.
- **Cost_Rate**: An hourly rate definition scoped to an organization. Can be assigned at the member level, or at the role-per-department level. Member-level rates take precedence over role-per-department rates.
- **Time_Policy**: Organization-level settings governing time tracking rules — required hours per day, overtime thresholds, whether approval is required, and the approval workflow configuration.
- **Time_Approval**: A record representing the approval status of a time entry. Tracks who approved/rejected, when, and optional rejection reason.
- **Timer_Session**: An in-progress timing session stored in Redis. Represents a running start/stop timer for a team member against a story.
- **Cost_Snapshot**: A pre-aggregated record of project cost data computed periodically. Used for expensive analytics queries instead of real-time aggregation.
- **Applicable_Rate**: The resolved hourly rate for a given time entry, determined by the rate precedence hierarchy: member rate → role-per-department rate → organization default rate.
- **Resource_Utilization**: The ratio of a team member's logged hours to the expected hours defined by the Time_Policy, calculated per project per organization.
- **Sprint_Velocity**: The sum of completed story points in a sprint, enriched with total logged hours from time entries for stories in that sprint.
- **WorkService**: The microservice (port 5003, database `nexus_work`) that owns stories, tasks, sprints, projects, and now time tracking and cost data.
- **Approval_Workflow**: The configured process by which time entries are reviewed. Can be set to `None` (auto-approved), `DeptLeadApproval` (department lead approves), or `ProjectLeadApproval` (project lead approves).

## Requirements

### Requirement 1: Create Time Entry (Manual)

**User Story:** As a team member, I want to manually log a time entry against a story, so that I can record work performed outside of the timer.

#### Acceptance Criteria

1. WHEN a team member submits a valid time entry to `POST /api/v1/time-entries` with `storyId`, `durationMinutes`, `date`, `isBillable`, and optional `notes`, THE WorkService SHALL create a Time_Entry record with status `Pending` and return HTTP 201 with the created Time_Entry.
2. WHEN the referenced `storyId` does not exist or belongs to a different organization, THE WorkService SHALL return HTTP 404 with error code `STORY_NOT_FOUND` (4001).
3. WHEN `durationMinutes` is less than or equal to zero, THE WorkService SHALL return HTTP 400 with error code `HOURS_MUST_BE_POSITIVE` (4035).
4. WHEN the Time_Policy for the organization has `ApprovalRequired` set to `false`, THE WorkService SHALL set the Time_Entry status to `Approved` immediately upon creation.
5. WHEN a Time_Entry is created, THE WorkService SHALL publish an activity log event to `outbox:work` containing the entry details.

### Requirement 2: Start/Stop Timer

**User Story:** As a team member, I want to start and stop a timer against a story, so that I can track time in real-time without manual calculation.

#### Acceptance Criteria

1. WHEN a team member calls `POST /api/v1/time-entries/timer/start` with a valid `storyId`, THE WorkService SHALL create a Timer_Session in Redis with key `timer:{userId}:{storyId}` containing the start timestamp and return HTTP 200.
2. WHEN a team member calls `POST /api/v1/time-entries/timer/stop`, THE WorkService SHALL retrieve the active Timer_Session from Redis, calculate the elapsed duration, create a Time_Entry record with the computed `durationMinutes`, delete the Timer_Session from Redis, and return HTTP 200 with the created Time_Entry.
3. WHEN a team member attempts to start a timer while an active Timer_Session already exists for that member, THE WorkService SHALL return HTTP 409 with error code `TIMER_ALREADY_ACTIVE` (4041).
4. WHEN a team member calls `GET /api/v1/time-entries/timer/status`, THE WorkService SHALL return the active Timer_Session details (storyId, startTime, elapsed seconds) or HTTP 204 if no timer is active.
5. WHEN the referenced `storyId` does not exist or belongs to a different organization, THE WorkService SHALL return HTTP 404 with error code `STORY_NOT_FOUND` (4001).
6. WHEN a team member calls stop and no active Timer_Session exists, THE WorkService SHALL return HTTP 400 with error code `NO_ACTIVE_TIMER` (4042).

### Requirement 3: Update and Delete Time Entry

**User Story:** As a team member, I want to update or delete my own time entries, so that I can correct mistakes in my logged time.

#### Acceptance Criteria

1. WHEN a team member calls `PUT /api/v1/time-entries/{timeEntryId}` with updated fields, THE WorkService SHALL update the Time_Entry and return HTTP 200 with the updated record.
2. WHEN a team member attempts to update a Time_Entry that belongs to a different member, THE WorkService SHALL return HTTP 403 with error code `INSUFFICIENT_PERMISSIONS` (4032).
3. WHEN a team member attempts to update a Time_Entry with status `Approved`, THE WorkService SHALL reset the status to `Pending` and trigger the approval workflow again.
4. WHEN a team member calls `DELETE /api/v1/time-entries/{timeEntryId}`, THE WorkService SHALL soft-delete the Time_Entry by setting `FlgStatus` to `D` and return HTTP 200.
5. WHEN a team member attempts to delete a Time_Entry that belongs to a different member, THE WorkService SHALL return HTTP 403 with error code `INSUFFICIENT_PERMISSIONS` (4032).
6. IF the `timeEntryId` does not exist, THEN THE WorkService SHALL return HTTP 404 with error code `TIME_ENTRY_NOT_FOUND` (4043).

### Requirement 4: List and Filter Time Entries

**User Story:** As a team member or manager, I want to list and filter time entries, so that I can review logged time across stories, projects, sprints, and team members.

#### Acceptance Criteria

1. WHEN a team member calls `GET /api/v1/time-entries` with optional query parameters (`storyId`, `projectId`, `sprintId`, `memberId`, `dateFrom`, `dateTo`, `isBillable`, `status`, `page`, `pageSize`), THE WorkService SHALL return a paginated list of Time_Entry records scoped to the organization.
2. THE WorkService SHALL support filtering by any combination of the provided query parameters.
3. WHEN no time entries match the filter criteria, THE WorkService SHALL return HTTP 200 with an empty list and `totalCount` of 0.
4. THE WorkService SHALL order results by `date` descending by default.

### Requirement 5: Time Entry Approval Workflow

**User Story:** As a department lead or project lead, I want to approve or reject time entries, so that logged time is validated before it counts toward project costs.

#### Acceptance Criteria

1. WHEN an approver calls `POST /api/v1/time-entries/{timeEntryId}/approve`, THE WorkService SHALL set the Time_Entry status to `Approved`, create a Time_Approval record with the approver's identity and timestamp, and return HTTP 200.
2. WHEN an approver calls `POST /api/v1/time-entries/{timeEntryId}/reject` with a `reason`, THE WorkService SHALL set the Time_Entry status to `Rejected`, create a Time_Approval record with the rejection reason, and return HTTP 200.
3. WHEN the Time_Policy `ApprovalWorkflow` is set to `DeptLeadApproval`, THE WorkService SHALL restrict approval actions to users with the DeptLead or OrgAdmin role in the time entry owner's department.
4. WHEN the Time_Policy `ApprovalWorkflow` is set to `ProjectLeadApproval`, THE WorkService SHALL restrict approval actions to the project lead of the story's project or users with the OrgAdmin role.
5. WHEN a user without the required approval role attempts to approve or reject, THE WorkService SHALL return HTTP 403 with error code `INSUFFICIENT_PERMISSIONS` (4032).
6. WHEN a Time_Entry is already in `Approved` or `Rejected` status and an approver attempts to change the status, THE WorkService SHALL allow the status change and create a new Time_Approval record (re-approval or override).
7. WHEN a Time_Entry is approved, THE WorkService SHALL publish a notification event to `outbox:work` for the time entry owner.

### Requirement 6: Cost Rate Management

**User Story:** As an organization admin, I want to define hourly cost rates at the member, role, and department levels, so that project costs can be calculated accurately.

#### Acceptance Criteria

1. WHEN an OrgAdmin calls `POST /api/v1/cost-rates` with `rateType` (`Member`, `RoleDepartment`, or `OrgDefault`), `hourlyRate`, and the appropriate scope identifiers (`memberId`, or `roleName` + `departmentId`), THE WorkService SHALL create a Cost_Rate record and return HTTP 201.
2. WHEN an OrgAdmin calls `PUT /api/v1/cost-rates/{costRateId}` with an updated `hourlyRate` and optional `effectiveFrom` date, THE WorkService SHALL update the Cost_Rate record and return HTTP 200.
3. WHEN an OrgAdmin calls `DELETE /api/v1/cost-rates/{costRateId}`, THE WorkService SHALL soft-delete the Cost_Rate by setting `FlgStatus` to `D` and return HTTP 200.
4. WHEN an OrgAdmin calls `GET /api/v1/cost-rates` with optional filters (`rateType`, `memberId`, `departmentId`, `roleName`), THE WorkService SHALL return a paginated list of active Cost_Rate records for the organization.
5. WHEN a non-OrgAdmin attempts to create, update, or delete a Cost_Rate, THE WorkService SHALL return HTTP 403 with error code `INSUFFICIENT_PERMISSIONS` (4032).
6. WHEN a duplicate Cost_Rate is submitted (same `rateType`, `memberId`, `roleName`, `departmentId` combination for the same organization), THE WorkService SHALL return HTTP 409 with error code `COST_RATE_DUPLICATE` (4044).
7. WHEN `hourlyRate` is less than or equal to zero, THE WorkService SHALL return HTTP 400 with error code `INVALID_COST_RATE` (4045).

### Requirement 7: Cost Rate Resolution (Applicable Rate)

**User Story:** As the platform, I want to resolve the correct hourly rate for a time entry using a defined precedence hierarchy, so that project cost calculations use the most specific rate available.

#### Acceptance Criteria

1. WHEN calculating the cost for a Time_Entry, THE WorkService SHALL resolve the Applicable_Rate using this precedence: (1) member-specific Cost_Rate for the entry's `memberId`, (2) role-per-department Cost_Rate matching the member's `roleName` and `departmentId`, (3) organization default Cost_Rate.
2. WHEN multiple Cost_Rate records exist for the same scope with different `effectiveFrom` dates, THE WorkService SHALL use the rate where `effectiveFrom` is the most recent date that is on or before the Time_Entry `date`.
3. WHEN no Cost_Rate exists at any level for a given Time_Entry, THE WorkService SHALL treat the cost as zero and log a warning.
4. THE WorkService SHALL resolve rates consistently such that for any given Time_Entry, the same inputs produce the same Applicable_Rate (deterministic resolution).

### Requirement 8: Time Policy Management

**User Story:** As an organization admin, I want to configure time tracking policies for my organization, so that time tracking rules are enforced consistently.

#### Acceptance Criteria

1. WHEN an OrgAdmin calls `PUT /api/v1/time-policies` with policy fields (`requiredHoursPerDay`, `overtimeThresholdHoursPerDay`, `approvalRequired`, `approvalWorkflow`, `maxDailyHours`), THE WorkService SHALL create or update the Time_Policy for the organization and return HTTP 200.
2. WHEN an OrgAdmin calls `GET /api/v1/time-policies`, THE WorkService SHALL return the current Time_Policy for the organization, or a default policy if none has been configured.
3. THE WorkService SHALL enforce the following defaults for a new organization: `requiredHoursPerDay` = 8, `overtimeThresholdHoursPerDay` = 10, `approvalRequired` = false, `approvalWorkflow` = `None`, `maxDailyHours` = 24.
4. WHEN a non-OrgAdmin attempts to update the Time_Policy, THE WorkService SHALL return HTTP 403 with error code `INSUFFICIENT_PERMISSIONS` (4032).
5. WHEN `requiredHoursPerDay` is less than or equal to zero or greater than 24, THE WorkService SHALL return HTTP 400 with error code `INVALID_TIME_POLICY` (4046).
6. WHEN `maxDailyHours` is less than `requiredHoursPerDay`, THE WorkService SHALL return HTTP 400 with error code `INVALID_TIME_POLICY` (4046).

### Requirement 9: Project Cost Calculation

**User Story:** As a project manager, I want to see the total cost of a project based on logged time and applicable rates, so that I can track budget consumption.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/projects/{projectId}/cost-summary`, THE WorkService SHALL return the total project cost calculated as the sum of (`durationMinutes` / 60 × Applicable_Rate) for all approved, billable Time_Entry records linked to stories in the project.
2. THE WorkService SHALL include in the response: `totalCost`, `totalBillableHours`, `totalNonBillableHours`, `costByMember` (array of member ID, name, hours, cost), and `costByDepartment` (array of department ID, name, hours, cost).
3. WHEN optional `dateFrom` and `dateTo` query parameters are provided, THE WorkService SHALL filter Time_Entry records to the specified date range.
4. WHEN no approved billable time entries exist for the project, THE WorkService SHALL return a cost summary with all values set to zero.
5. FOR ALL project cost calculations, THE WorkService SHALL produce the same total cost regardless of the order in which Time_Entry records are processed (confluence property).

### Requirement 10: Resource Utilization

**User Story:** As a manager, I want to see resource utilization metrics per project per organization, so that I can identify over- and under-utilized team members.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/projects/{projectId}/utilization` with optional `dateFrom` and `dateTo` parameters, THE WorkService SHALL return utilization data for each team member who has logged time on the project.
2. THE WorkService SHALL calculate utilization as: (`totalLoggedHours` / `expectedHours`) × 100, where `expectedHours` is derived from the Time_Policy `requiredHoursPerDay` × number of working days in the date range.
3. THE WorkService SHALL include in the response: `memberId`, `memberName`, `totalLoggedHours`, `expectedHours`, `utilizationPercentage`, `billableHours`, `nonBillableHours`.
4. WHEN a team member has no logged time in the date range, THE WorkService SHALL include the member with `totalLoggedHours` of 0 and `utilizationPercentage` of 0.

### Requirement 11: Sprint Velocity Enrichment

**User Story:** As a scrum master, I want sprint velocity data enriched with time tracking information, so that I can correlate story points with actual effort.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/sprints/{sprintId}/velocity`, THE WorkService SHALL return the sprint velocity including: `totalStoryPoints` (sum of completed story points), `totalLoggedHours` (sum of approved time entry hours for stories in the sprint), `averageHoursPerPoint` (totalLoggedHours / totalStoryPoints), and `completedStoryCount`.
2. WHEN the sprint has no completed stories, THE WorkService SHALL return velocity data with all values set to zero.
3. WHEN `totalStoryPoints` is zero and `totalLoggedHours` is greater than zero, THE WorkService SHALL return `averageHoursPerPoint` as null to avoid division by zero.

### Requirement 12: Cost Snapshot Generation

**User Story:** As the platform, I want to pre-aggregate project cost data periodically, so that expensive analytics queries are served from snapshots instead of real-time computation.

#### Acceptance Criteria

1. WHEN the Cost_Snapshot generation process runs, THE WorkService SHALL compute and store a Cost_Snapshot record for each active project containing: `projectId`, `totalCost`, `totalBillableHours`, `totalNonBillableHours`, `snapshotDate`, and `periodStart`/`periodEnd`.
2. THE WorkService SHALL expose `GET /api/v1/projects/{projectId}/cost-snapshots` to return historical Cost_Snapshot records with optional `dateFrom` and `dateTo` filters.
3. THE WorkService SHALL support both real-time cost queries (Requirement 9) and snapshot-based queries, with the API clearly distinguishing between the two via separate endpoints.
4. WHEN a Cost_Snapshot is generated, THE WorkService SHALL ensure the snapshot total matches the real-time calculation for the same period (idempotence: generating the snapshot twice for the same period produces the same result).

### Requirement 13: Billable vs Non-Billable Categorization

**User Story:** As a team member, I want to categorize my time entries as billable or non-billable, so that project cost reports accurately reflect chargeable work.

#### Acceptance Criteria

1. THE WorkService SHALL store an `isBillable` boolean flag on each Time_Entry, defaulting to `true`.
2. WHEN a Time_Entry is created or updated, THE WorkService SHALL accept the `isBillable` field and persist the value.
3. WHEN calculating project costs (Requirement 9), THE WorkService SHALL include only Time_Entry records where `isBillable` is `true`.
4. WHEN listing time entries (Requirement 4), THE WorkService SHALL support filtering by `isBillable` value.

### Requirement 14: Time Entry Validation Against Policy

**User Story:** As the platform, I want to validate time entries against the organization's time policy, so that entries exceeding policy limits are flagged.

#### Acceptance Criteria

1. WHEN a Time_Entry is created and the total logged hours for the member on that `date` (including the new entry) exceeds the Time_Policy `maxDailyHours`, THE WorkService SHALL return HTTP 400 with error code `DAILY_HOURS_EXCEEDED` (4047).
2. WHEN a Time_Entry is created and the total logged hours for the member on that `date` (including the new entry) exceeds the Time_Policy `overtimeThresholdHoursPerDay`, THE WorkService SHALL flag the Time_Entry with `isOvertime` = true.
3. THE WorkService SHALL calculate daily totals by summing `durationMinutes` of all active (non-deleted) Time_Entry records for the member on the same `date`.

### Requirement 15: Time Tracking Error Codes

**User Story:** As the platform, I want dedicated error codes for time tracking operations, so that clients can handle errors programmatically.

#### Acceptance Criteria

1. THE WorkService SHALL define and return the following error codes for time tracking operations:

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| TIMER_ALREADY_ACTIVE | 4041 | 409 | Member already has a running timer |
| NO_ACTIVE_TIMER | 4042 | 400 | No running timer to stop |
| TIME_ENTRY_NOT_FOUND | 4043 | 404 | Time entry does not exist |
| COST_RATE_DUPLICATE | 4044 | 409 | Duplicate cost rate for same scope |
| INVALID_COST_RATE | 4045 | 400 | Hourly rate must be greater than zero |
| INVALID_TIME_POLICY | 4046 | 400 | Time policy field value is invalid |
| DAILY_HOURS_EXCEEDED | 4047 | 400 | Total daily hours exceed policy maximum |

2. THE WorkService SHALL return all error codes in the standard `ApiResponse<T>` envelope with `ErrorCode`, `ErrorValue`, and `CorrelationId` fields.
