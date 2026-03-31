# Requirements Document — Analytics & Reporting Service

## Introduction

This feature adds an analytics and reporting layer (Phase 2) to the Nexus 2.0 WorkService, building on top of the existing time tracking, cost data, sprint, story, and project entities from Phase 1. The feature provides eight analytics domains: sprint velocity trends, resource management, resource utilization, project cost tracking, project health scoring, risk management, backlog dependency analysis, and bug tracking metrics.

The approach is hybrid: real-time queries serve simple metrics (current bug counts, active resource allocation, dependency graphs), while pre-aggregated snapshots serve expensive calculations (velocity trends, health scores, cost trends) that are recalculated on sprint close or on a configurable schedule.

Four new domain entities are introduced: `RiskRegister`, `ProjectHealthSnapshot`, `VelocitySnapshot`, and `ResourceAllocationSnapshot`. All entities are organization-scoped (`IOrganizationEntity`), soft-deletable (`FlgStatus` where applicable), and follow the established subfolder convention for Domain interfaces, Infrastructure repositories, and services.

## Glossary

- **WorkService**: The microservice (port 5003, database `nexus_work`) that owns stories, tasks, sprints, projects, time entries, cost rates, and now analytics/reporting data.
- **Velocity_Snapshot**: A pre-aggregated record of sprint velocity data containing committed story points, completed story points, total logged hours, and average hours per point for a specific sprint within an organization.
- **Project_Health_Snapshot**: A pre-aggregated composite health score for a project, derived from velocity trends, bug rates, overdue item counts, and risk counts. Stored periodically for trend analysis.
- **Resource_Allocation_Snapshot**: A pre-aggregated record of team member allocation data across projects, capturing capacity, assigned hours, and utilization percentage for a given period.
- **Risk_Register**: A record representing a risk linked to a sprint and project, with severity, likelihood, mitigation status, and optional description. Used for risk tracking and project health scoring.
- **Health_Score**: A composite numeric score (0–100) representing overall project health, calculated from weighted sub-scores: velocity trend, bug rate, overdue item ratio, and risk severity.
- **Dependency_Chain**: An ordered sequence of stories connected by `blocks` / `is_blocked_by` StoryLink relationships, representing a critical path through the backlog.
- **Burn_Rate**: The rate at which project budget is being consumed, calculated as total cost divided by elapsed project duration, expressed as cost per day or per sprint.
- **Bug_Rate**: The ratio of stories with type `Bug` to total stories in a sprint, expressed as a percentage.
- **Committed_Points**: The total story points of all stories assigned to a sprint at sprint start (when status transitions from `Planning` to `Active`).
- **Completed_Points**: The total story points of stories with status `Done` in a sprint at sprint close.
- **Snapshot_Trigger**: The event or schedule that initiates snapshot recalculation — either a sprint status change (close/complete) or a periodic timer (configurable interval).
- **Analytics_Service**: The domain service responsible for orchestrating analytics calculations, snapshot generation, and serving analytics query endpoints.
- **ProfileServiceClient**: The existing typed HTTP client used to fetch team member details (name, department, role) from ProfileService for resource-related analytics.

## Requirements

### Requirement 1: Sprint Velocity Analytics

**User Story:** As a scrum master, I want to view velocity trends across sprints for my organization, so that I can assess team performance and forecast capacity.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/analytics/velocity?projectId={projectId}` with optional `sprintCount` (default 10), THE Analytics_Service SHALL return a list of Velocity_Snapshot records for the most recent completed sprints in the specified project, ordered by sprint end date descending.
2. THE Analytics_Service SHALL include in each Velocity_Snapshot: `sprintId`, `sprintName`, `startDate`, `endDate`, `committedPoints`, `completedPoints`, `totalLoggedHours`, `averageHoursPerPoint`, and `completedStoryCount`.
3. WHEN a sprint is completed (status transitions to `Completed`), THE Analytics_Service SHALL generate or update a Velocity_Snapshot for that sprint containing the committed points (story points of all stories assigned at sprint start), completed points (story points of stories with status `Done`), total approved logged hours from time entries, and average hours per point.
4. WHEN no completed sprints exist for the project, THE Analytics_Service SHALL return HTTP 200 with an empty list.
5. WHEN `sprintCount` is less than 1 or greater than 50, THE Analytics_Service SHALL return HTTP 400 with error code `INVALID_ANALYTICS_PARAMETER` (4060).
6. FOR ALL Velocity_Snapshot records, regenerating the snapshot for the same sprint SHALL produce identical values (idempotence property).

### Requirement 2: Resource Management Analytics

**User Story:** As a project manager, I want to view team member allocation and workload distribution across projects, so that I can plan capacity and balance workloads.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/analytics/resource-management` with optional `dateFrom`, `dateTo`, and `departmentId` parameters, THE Analytics_Service SHALL return allocation data for each team member in the organization who has logged time in the specified period.
2. THE Analytics_Service SHALL include for each member: `memberId`, `memberName`, `departmentId`, `totalLoggedHours`, `projectBreakdown` (array of project ID, project name, hours logged, percentage of total), and `capacityUtilizationPercentage`.
3. THE Analytics_Service SHALL calculate `capacityUtilizationPercentage` as (`totalLoggedHours` / `expectedHours`) × 100, where `expectedHours` is derived from the Time_Policy `requiredHoursPerDay` multiplied by the number of working days in the date range.
4. WHEN a team member has logged time on multiple projects, THE Analytics_Service SHALL include all projects in the `projectBreakdown` array sorted by hours descending.
5. WHEN no time entries exist for the specified period, THE Analytics_Service SHALL return HTTP 200 with an empty list.
6. THE Analytics_Service SHALL fetch team member names and department details from ProfileService via the existing ProfileServiceClient.

### Requirement 3: Resource Utilization Per Project

**User Story:** As a project lead, I want to see how much of each team member's capacity is being used on a specific project, so that I can identify over- and under-utilized resources.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/analytics/resource-utilization?projectId={projectId}` with optional `dateFrom` and `dateTo` parameters, THE Analytics_Service SHALL return utilization data for each team member who has logged time on the specified project.
2. THE Analytics_Service SHALL include for each member: `memberId`, `memberName`, `totalLoggedHours`, `expectedHours`, `utilizationPercentage`, `billableHours`, `nonBillableHours`, and `overtimeHours`.
3. WHEN a Resource_Allocation_Snapshot exists for the requested period, THE Analytics_Service SHALL serve data from the snapshot instead of computing in real-time.
4. WHEN a Resource_Allocation_Snapshot does not exist, THE Analytics_Service SHALL compute utilization in real-time from time entry data.
5. WHEN the snapshot generation process runs, THE Analytics_Service SHALL generate a Resource_Allocation_Snapshot for each active project containing per-member utilization data for the snapshot period.
6. FOR ALL Resource_Allocation_Snapshot records, the snapshot utilization values SHALL match the real-time calculation for the same period and inputs (consistency property).

### Requirement 4: Project Cost Analytics

**User Story:** As a project manager, I want to track total project cost with budget vs actual comparison and burn rate, so that I can monitor budget consumption and forecast overruns.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/analytics/project-cost?projectId={projectId}` with optional `dateFrom` and `dateTo` parameters, THE Analytics_Service SHALL return cost analytics including: `totalCost`, `totalBillableHours`, `totalNonBillableHours`, `burnRatePerDay`, `costByMember` (array), and `costByDepartment` (array).
2. THE Analytics_Service SHALL calculate `burnRatePerDay` as `totalCost` divided by the number of elapsed working days in the date range.
3. WHEN historical Cost_Snapshot records exist for the project, THE Analytics_Service SHALL include a `costTrend` array containing snapshot date and total cost for each snapshot, ordered by date ascending.
4. WHEN no approved billable time entries exist for the project in the date range, THE Analytics_Service SHALL return cost analytics with all monetary values set to zero and `burnRatePerDay` set to zero.
5. FOR ALL project cost calculations, THE Analytics_Service SHALL produce the same total cost regardless of the order in which time entry records are processed (confluence property).

### Requirement 5: Project Health Score

**User Story:** As a project manager, I want a composite health score for my project derived from multiple metrics, so that I can quickly assess overall project status.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/analytics/project-health?projectId={projectId}`, THE Analytics_Service SHALL return the most recent Project_Health_Snapshot containing: `overallScore` (0–100), `velocityScore`, `bugRateScore`, `overdueScore`, `riskScore`, `snapshotDate`, and `trend` (improving, stable, declining).
2. THE Analytics_Service SHALL calculate `velocityScore` (0–100) based on the ratio of completed points to committed points across the last 3 completed sprints, where 100 means all committed points were completed.
3. THE Analytics_Service SHALL calculate `bugRateScore` (0–100) as 100 minus the bug rate percentage, where bug rate is the count of open Bug-type stories divided by total active stories in the project.
4. THE Analytics_Service SHALL calculate `overdueScore` (0–100) as 100 minus the percentage of stories past their `dueDate` that are not in `Done` status.
5. THE Analytics_Service SHALL calculate `riskScore` (0–100) based on the count and severity of active Risk_Register entries for the project, where fewer and lower-severity risks yield a higher score.
6. THE Analytics_Service SHALL calculate `overallScore` as a weighted average: `velocityScore` × 0.30 + `bugRateScore` × 0.25 + `overdueScore` × 0.25 + `riskScore` × 0.20.
7. THE Analytics_Service SHALL determine `trend` by comparing the current `overallScore` to the previous snapshot: `improving` if current is more than 5 points higher, `declining` if more than 5 points lower, `stable` otherwise.
8. WHEN a sprint is completed or the periodic snapshot timer fires, THE Analytics_Service SHALL generate a new Project_Health_Snapshot for each active project.
9. WHEN a user calls `GET /api/v1/analytics/project-health?projectId={projectId}&history=true`, THE Analytics_Service SHALL return the last 10 Project_Health_Snapshot records ordered by `snapshotDate` descending.
10. WHEN no data exists to calculate a sub-score (e.g., no sprints completed), THE Analytics_Service SHALL assign a neutral score of 50 for that component.

### Requirement 6: Risk Register Management

**User Story:** As a project manager, I want to track risks per sprint per project with severity, likelihood, and mitigation status, so that I can manage project risks and feed them into health scoring.

#### Acceptance Criteria

1. WHEN an authorized user calls `POST /api/v1/analytics/risks` with `projectId`, `sprintId` (optional), `title`, `description`, `severity` (Low, Medium, High, Critical), `likelihood` (Low, Medium, High), and `mitigationStatus` (Open, Mitigating, Mitigated, Accepted), THE Analytics_Service SHALL create a Risk_Register record and return HTTP 201.
2. WHEN an authorized user calls `PUT /api/v1/analytics/risks/{riskId}` with updated fields, THE Analytics_Service SHALL update the Risk_Register record and return HTTP 200.
3. WHEN an authorized user calls `DELETE /api/v1/analytics/risks/{riskId}`, THE Analytics_Service SHALL soft-delete the Risk_Register record by setting `FlgStatus` to `D` and return HTTP 200.
4. WHEN a user calls `GET /api/v1/analytics/risks?projectId={projectId}` with optional `sprintId`, `severity`, and `mitigationStatus` filters, THE Analytics_Service SHALL return a paginated list of active Risk_Register records matching the filters.
5. WHEN `severity` is not one of `Low`, `Medium`, `High`, `Critical`, THE Analytics_Service SHALL return HTTP 400 with error code `INVALID_RISK_SEVERITY` (4061).
6. WHEN `likelihood` is not one of `Low`, `Medium`, `High`, THE Analytics_Service SHALL return HTTP 400 with error code `INVALID_RISK_LIKELIHOOD` (4062).
7. WHEN `mitigationStatus` is not one of `Open`, `Mitigating`, `Mitigated`, `Accepted`, THE Analytics_Service SHALL return HTTP 400 with error code `INVALID_MITIGATION_STATUS` (4063).
8. WHEN a user without DeptLead or OrgAdmin role attempts to create, update, or delete a Risk_Register, THE Analytics_Service SHALL return HTTP 403 with error code `INSUFFICIENT_PERMISSIONS` (4032).
9. IF the `riskId` does not exist, THEN THE Analytics_Service SHALL return HTTP 404 with error code `RISK_NOT_FOUND` (4064).

### Requirement 7: Backlog Dependency Analysis

**User Story:** As a scrum master, I want to analyze story dependencies in the backlog using existing story links, so that I can detect blocking chains and plan sprint work effectively.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/analytics/dependencies?projectId={projectId}`, THE Analytics_Service SHALL return a dependency analysis containing: `totalDependencies`, `blockingChains` (array of dependency chains), and `blockedStories` (array of stories that are currently blocked).
2. THE Analytics_Service SHALL identify a `blockingChain` by traversing `blocks` / `is_blocked_by` StoryLink relationships starting from stories with no incoming `blocks` links, following the chain until a story with no outgoing `blocks` links is reached.
3. THE Analytics_Service SHALL include for each chain: `chainLength`, `stories` (ordered array of story ID, story key, title, status, assignee), and `criticalPath` (boolean, true if any story in the chain is in an active sprint).
4. THE Analytics_Service SHALL include in `blockedStories`: stories that have at least one incoming `is_blocked_by` link where the blocking story is not in `Done` status.
5. WHEN no `blocks` / `is_blocked_by` links exist for the project, THE Analytics_Service SHALL return an analysis with `totalDependencies` of 0 and empty arrays.
6. THE Analytics_Service SHALL detect circular dependencies and include them in a `circularDependencies` array, each containing the story IDs forming the cycle.
7. WHEN optional `sprintId` parameter is provided, THE Analytics_Service SHALL filter the analysis to only include stories assigned to the specified sprint.

### Requirement 8: Bug Metrics Per Sprint

**User Story:** As a quality lead, I want to track bug metrics per sprint per project, so that I can monitor quality trends and identify problematic areas.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/analytics/bugs?projectId={projectId}` with optional `sprintId` parameter, THE Analytics_Service SHALL return bug metrics including: `totalBugs`, `openBugs`, `closedBugs`, `reopenedBugs`, `bugRate` (percentage of bug stories vs total stories), and `bugsBySeverity` (breakdown by priority).
2. THE Analytics_Service SHALL identify bug stories by filtering stories where the `Priority` field indicates a bug type or where the story was created with a bug classification.
3. WHEN `sprintId` is provided, THE Analytics_Service SHALL scope bug metrics to stories assigned to that sprint via SprintStory records.
4. WHEN `sprintId` is not provided, THE Analytics_Service SHALL return aggregate bug metrics across all sprints for the project.
5. THE Analytics_Service SHALL include a `bugTrend` array containing bug counts per sprint for the last 10 completed sprints, ordered by sprint end date ascending.
6. WHEN no bug stories exist for the specified scope, THE Analytics_Service SHALL return metrics with all counts set to zero and `bugRate` set to zero.

### Requirement 9: Snapshot Generation Scheduling

**User Story:** As the platform, I want analytics snapshots to be generated on sprint close and on a configurable schedule, so that expensive calculations are pre-computed and served efficiently.

#### Acceptance Criteria

1. WHEN a sprint status transitions to `Completed`, THE Analytics_Service SHALL trigger snapshot generation for: Velocity_Snapshot (for the completed sprint), Project_Health_Snapshot (for the sprint's project), and Resource_Allocation_Snapshot (for the sprint's project and period).
2. THE Analytics_Service SHALL run a periodic background process (configurable interval, default every 6 hours) that generates Project_Health_Snapshot and Resource_Allocation_Snapshot records for all active projects.
3. WHEN the snapshot generation process encounters an error for a specific project, THE Analytics_Service SHALL log the error and continue processing remaining projects without failing the entire batch.
4. FOR ALL snapshot types, regenerating a snapshot for the same inputs SHALL produce identical results (idempotence property).
5. THE Analytics_Service SHALL store snapshot generation metadata (last run time, projects processed, errors encountered) accessible via `GET /api/v1/analytics/snapshot-status` for operational monitoring.

### Requirement 10: Analytics Dashboard Summary

**User Story:** As a project manager, I want a single endpoint that returns a summary of all analytics for a project, so that I can populate a dashboard without making multiple API calls.

#### Acceptance Criteria

1. WHEN a user calls `GET /api/v1/analytics/dashboard?projectId={projectId}`, THE Analytics_Service SHALL return a consolidated response containing: latest `projectHealth` (overall score and trend), latest `velocitySnapshot` (last completed sprint), `activeBugCount`, `activeRiskCount`, `blockedStoryCount`, `totalProjectCost`, and `burnRatePerDay`.
2. THE Analytics_Service SHALL serve dashboard data from the most recent snapshots where available, falling back to real-time calculation only for metrics without snapshots.
3. WHEN the project has no analytics data, THE Analytics_Service SHALL return a dashboard with neutral/zero values for all metrics.

### Requirement 11: Analytics Error Codes

**User Story:** As the platform, I want dedicated error codes for analytics operations, so that clients can handle errors programmatically.

#### Acceptance Criteria

1. THE Analytics_Service SHALL define and return the following error codes for analytics operations:

| Code | Value | HTTP | Description |
|------|-------|------|-------------|
| INVALID_ANALYTICS_PARAMETER | 4060 | 400 | Analytics query parameter value is invalid |
| INVALID_RISK_SEVERITY | 4061 | 400 | Risk severity must be Low, Medium, High, or Critical |
| INVALID_RISK_LIKELIHOOD | 4062 | 400 | Risk likelihood must be Low, Medium, or High |
| INVALID_MITIGATION_STATUS | 4063 | 400 | Mitigation status must be Open, Mitigating, Mitigated, or Accepted |
| RISK_NOT_FOUND | 4064 | 404 | Risk register entry does not exist |
| SNAPSHOT_GENERATION_FAILED | 4065 | 500 | Snapshot generation failed for a project |

2. THE Analytics_Service SHALL return all error codes in the standard `ApiResponse<T>` envelope with `ErrorCode`, `ErrorValue`, and `CorrelationId` fields.
