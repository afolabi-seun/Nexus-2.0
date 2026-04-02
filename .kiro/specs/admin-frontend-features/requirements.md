# Requirements Document

## Introduction

This feature adds missing admin and management pages to the Nexus 2.0 React + TypeScript frontend. It covers four areas: an Audit Logs Viewer for PlatformAdmin and OrgAdmin roles, an Error Logs Viewer for OrgAdmin, a Reference Data Management page for OrgAdmin, and sprint editing capability on the existing SprintDetailPage for DeptLead and above. All new pages follow existing frontend patterns (AdminLayout/AppShell, DataTable, Pagination, Modal, RoleGuard, createApiClient, Tailwind styling).

## Glossary

- **Audit_Logs_Page**: A new frontend page that displays audit log entries fetched from the utility service, with filtering and pagination.
- **Error_Logs_Page**: A new frontend page that displays error log entries fetched from the utility service, with filtering, pagination, and expandable stack traces.
- **Reference_Data_Page**: A new frontend page with tabs for managing department types, priority levels, task types, and workflow states.
- **Sprint_Edit_Modal**: A modal dialog on the existing SprintDetailPage that allows editing sprint name, goal, and dates.
- **PlatformAdmin**: A super-admin role that manages the entire platform; uses AdminLayout.
- **OrgAdmin**: An organization-level admin role; uses AppShell with RoleGuard.
- **DeptLead**: A department lead role with elevated permissions within an organization.
- **Utility_API_Client**: The existing `utilityApi` module (`src/frontend/src/api/utilityApi.ts`) that communicates with the utility backend service.
- **Work_API_Client**: The existing `workApi` module (`src/frontend/src/api/workApi.ts`) that communicates with the work backend service.
- **DataTable**: The existing reusable table component (`DataTable`) used across the application.
- **Pagination_Component**: The existing `Pagination` component providing page navigation and page-size selection.
- **Modal_Component**: The existing `Modal` component providing accessible dialog overlays.
- **RoleGuard**: The existing route guard component that restricts access based on user roles.
- **Badge_Component**: The existing `Badge` component for rendering status and priority indicators.

## Requirements

### Requirement 1: Audit Logs Page — Live Logs Display

**User Story:** As a PlatformAdmin or OrgAdmin, I want to view live audit log entries in a paginated table, so that I can monitor recent system activity.

#### Acceptance Criteria

1. WHEN the user navigates to the audit logs route, THE Audit_Logs_Page SHALL fetch audit log entries from `GET /api/v1/audit-logs` using the Utility_API_Client and display them in a DataTable.
2. THE Audit_Logs_Page SHALL display the following columns for each audit log entry: action, entityType, entityId, actorName, ipAddress, details, and dateCreated.
3. THE Audit_Logs_Page SHALL provide pagination using the Pagination_Component with configurable page size (10, 20, 50).
4. WHEN the page loads, THE Audit_Logs_Page SHALL default to page 1 with a page size of 20.

### Requirement 2: Audit Logs Page — Filtering

**User Story:** As a PlatformAdmin or OrgAdmin, I want to filter audit logs by service name, action, entity type, user, and date range, so that I can find specific audit events.

#### Acceptance Criteria

1. THE Audit_Logs_Page SHALL provide filter controls for: action (dropdown), entityType (dropdown), actorId (dropdown or text input), dateFrom (date input), and dateTo (date input).
2. WHEN the user changes any filter value, THE Audit_Logs_Page SHALL reset pagination to page 1 and re-fetch audit logs with the updated filter parameters.
3. WHEN the user clears all filters, THE Audit_Logs_Page SHALL fetch unfiltered audit logs from page 1.

### Requirement 3: Audit Logs Page — Archive Toggle

**User Story:** As a PlatformAdmin or OrgAdmin, I want to toggle between live and archived audit logs, so that I can review historical audit data.

#### Acceptance Criteria

1. THE Audit_Logs_Page SHALL provide a toggle control (e.g., button group or switch) to select between "Live" and "Archived" log views.
2. WHEN the user selects "Archived", THE Audit_Logs_Page SHALL fetch audit logs from `GET /api/v1/audit-logs/archive` instead of `GET /api/v1/audit-logs`.
3. WHEN the user toggles between live and archived views, THE Audit_Logs_Page SHALL reset pagination to page 1 and preserve the current filter values.
4. THE Utility_API_Client SHALL expose a `getArchivedAuditLogs` method that calls `GET /api/v1/audit-logs/archive` with the same filter and pagination parameters as `getAuditLogs`.

### Requirement 4: Audit Logs Page — Routing and Access Control

**User Story:** As a PlatformAdmin or OrgAdmin, I want the audit logs page to be accessible from my respective layout with proper role restrictions, so that only authorized users can view audit data.

#### Acceptance Criteria

1. THE router SHALL register the route `/admin/audit-logs` under the PlatformAdmin AdminLayout section, guarded by `RoleGuard` with `allowedRoles={['PlatformAdmin']}`.
2. THE router SHALL register the route `/audit-logs` under the OrgAdmin AppShell section, guarded by `RoleGuard` with `allowedRoles={['OrgAdmin']}`.
3. WHEN an unauthorized user attempts to access either audit logs route, THE RoleGuard SHALL redirect the user away from the page.


### Requirement 5: Error Logs Page — Display and Pagination

**User Story:** As an OrgAdmin, I want to view error log entries in a paginated table with severity badges, so that I can monitor and investigate application errors.

#### Acceptance Criteria

1. WHEN the user navigates to the error logs route, THE Error_Logs_Page SHALL fetch error log entries from `GET /api/v1/error-logs` using the Utility_API_Client and display them in a DataTable.
2. THE Error_Logs_Page SHALL display the following columns: serviceName, errorCode, severity, message, stackTrace (truncated), and dateCreated.
3. THE Error_Logs_Page SHALL render severity values using the Badge_Component with distinct visual styles for each severity level (e.g., Critical, Error, Warning, Info).
4. THE Error_Logs_Page SHALL provide pagination using the Pagination_Component with configurable page size (10, 20, 50).
5. THE Utility_API_Client SHALL expose a `getErrorLogs` method that calls `GET /api/v1/error-logs` with filter and pagination parameters.

### Requirement 6: Error Logs Page — Expandable Stack Traces

**User Story:** As an OrgAdmin, I want to expand individual error log rows to view full stack traces, so that I can diagnose issues without leaving the page.

#### Acceptance Criteria

1. WHEN an error log entry contains a non-empty stackTrace, THE Error_Logs_Page SHALL display an expand/collapse toggle on that row.
2. WHEN the user clicks the expand toggle, THE Error_Logs_Page SHALL reveal the full stack trace text in a preformatted block below the row.
3. WHEN the user clicks the collapse toggle, THE Error_Logs_Page SHALL hide the stack trace block.

### Requirement 7: Error Logs Page — Filtering

**User Story:** As an OrgAdmin, I want to filter error logs by service name, error code, severity, and date range, so that I can narrow down specific errors.

#### Acceptance Criteria

1. THE Error_Logs_Page SHALL provide filter controls for: serviceName (dropdown), errorCode (text input), severity (dropdown), dateFrom (date input), and dateTo (date input).
2. WHEN the user changes any filter value, THE Error_Logs_Page SHALL reset pagination to page 1 and re-fetch error logs with the updated filter parameters.
3. WHEN the user clears all filters, THE Error_Logs_Page SHALL fetch unfiltered error logs from page 1.

### Requirement 8: Error Logs Page — Routing and Access Control

**User Story:** As an OrgAdmin, I want the error logs page to be accessible from the organization settings area, so that I can find it alongside other admin tools.

#### Acceptance Criteria

1. THE router SHALL register the route `/settings/error-logs` under the OrgAdmin AppShell section, guarded by `RoleGuard` with `allowedRoles={['OrgAdmin']}`.
2. WHEN an unauthorized user attempts to access the error logs route, THE RoleGuard SHALL redirect the user away from the page.

### Requirement 9: Reference Data Page — Tabbed Layout and Read Display

**User Story:** As an OrgAdmin, I want to view reference data (department types, priority levels, task types, workflow states) organized in tabs, so that I can manage organizational configuration in one place.

#### Acceptance Criteria

1. WHEN the user navigates to the reference data route, THE Reference_Data_Page SHALL display a tabbed interface with four tabs: "Department Types", "Priority Levels", "Task Types", and "Workflow States".
2. WHEN a tab is selected, THE Reference_Data_Page SHALL display the corresponding reference data list in a DataTable.
3. THE Reference_Data_Page SHALL fetch department types from `GET /api/v1/reference/department-types` using the Utility_API_Client.
4. THE Reference_Data_Page SHALL fetch priority levels from `GET /api/v1/reference/priority-levels` using the Utility_API_Client.
5. THE Reference_Data_Page SHALL fetch task types from `GET /api/v1/reference/task-types` using the Utility_API_Client.
6. THE Reference_Data_Page SHALL fetch workflow states from `GET /api/v1/reference/workflow-states` using the Utility_API_Client.
7. THE Utility_API_Client SHALL expose individual methods for each reference data endpoint: `getDepartmentTypes`, `getPriorityLevels`, `getTaskTypes`, and `getWorkflowStates`.

### Requirement 10: Reference Data Page — Create Department Types and Priority Levels

**User Story:** As an OrgAdmin, I want to create new department types and priority levels, so that I can customize organizational reference data.

#### Acceptance Criteria

1. WHEN the "Department Types" tab is active, THE Reference_Data_Page SHALL display an "Add Department Type" button.
2. WHEN the user clicks "Add Department Type", THE Reference_Data_Page SHALL open a Modal_Component with a form containing fields for code and name.
3. WHEN the user submits a valid department type form, THE Reference_Data_Page SHALL send a POST request to `POST /api/v1/reference/department-types` using the Utility_API_Client and refresh the department types list on success.
4. WHEN the "Priority Levels" tab is active, THE Reference_Data_Page SHALL display an "Add Priority Level" button.
5. WHEN the user clicks "Add Priority Level", THE Reference_Data_Page SHALL open a Modal_Component with a form containing fields for code, name, and level (numeric).
6. WHEN the user submits a valid priority level form, THE Reference_Data_Page SHALL send a POST request to `POST /api/v1/reference/priority-levels` using the Utility_API_Client and refresh the priority levels list on success.
7. THE Utility_API_Client SHALL expose `createDepartmentType` and `createPriorityLevel` methods for the respective POST endpoints.

### Requirement 11: Reference Data Page — Read-Only Tabs

**User Story:** As an OrgAdmin, I want to view task types and workflow states as read-only lists, so that I can understand the system configuration without accidentally modifying it.

#### Acceptance Criteria

1. WHILE the "Task Types" tab is active, THE Reference_Data_Page SHALL display task types in a read-only DataTable without create or edit controls.
2. WHILE the "Workflow States" tab is active, THE Reference_Data_Page SHALL display workflow states in a read-only DataTable without create or edit controls, showing entityType, status, and validTransitions columns.

### Requirement 12: Reference Data Page — Routing and Access Control

**User Story:** As an OrgAdmin, I want the reference data page to be accessible from the organization settings area, so that I can manage configuration alongside other settings.

#### Acceptance Criteria

1. THE router SHALL register the route `/settings/reference-data` under the OrgAdmin AppShell section, guarded by `RoleGuard` with `allowedRoles={['OrgAdmin']}`.
2. WHEN an unauthorized user attempts to access the reference data route, THE RoleGuard SHALL redirect the user away from the page.

### Requirement 13: Sprint Edit — Edit Button and Modal

**User Story:** As a DeptLead or OrgAdmin, I want to edit sprint details (name, goal, start date, end date) from the sprint detail page, so that I can adjust sprint parameters as planning evolves.

#### Acceptance Criteria

1. WHILE the user has the role DeptLead or OrgAdmin, THE SprintDetailPage SHALL display an "Edit" button in the sprint header area.
2. WHEN the user clicks the "Edit" button, THE SprintDetailPage SHALL open a Sprint_Edit_Modal pre-populated with the current sprint name, goal, startDate, and endDate.
3. WHILE the user has the role Member or Viewer, THE SprintDetailPage SHALL hide the "Edit" button.

### Requirement 14: Sprint Edit — Form Validation and Submission

**User Story:** As a DeptLead or OrgAdmin, I want sprint edit form inputs to be validated before submission, so that invalid data is not sent to the backend.

#### Acceptance Criteria

1. THE Sprint_Edit_Modal SHALL validate that the sprint name is non-empty.
2. THE Sprint_Edit_Modal SHALL validate that the start date is before the end date.
3. IF the user submits the form with invalid data, THEN THE Sprint_Edit_Modal SHALL display inline validation error messages and prevent submission.
4. WHEN the user submits a valid form, THE Sprint_Edit_Modal SHALL send a PUT request to `PUT /api/v1/sprints/{id}` using the Work_API_Client with the updated name, goal, startDate, and endDate.
5. WHEN the update request succeeds, THE Sprint_Edit_Modal SHALL close, display a success toast notification, and refresh the sprint detail data.
6. IF the update request fails, THEN THE Sprint_Edit_Modal SHALL display an error toast notification with the mapped error code message.
7. THE Work_API_Client SHALL expose an `updateSprint` method that calls `PUT /api/v1/sprints/{id}` with an update payload.

### Requirement 15: Error Handling — Loading and Error States

**User Story:** As any authorized user, I want to see loading indicators while data is being fetched and clear error messages when requests fail, so that I understand the current state of the page.

#### Acceptance Criteria

1. WHILE data is being fetched, THE Audit_Logs_Page SHALL display a SkeletonLoader.
2. WHILE data is being fetched, THE Error_Logs_Page SHALL display a SkeletonLoader.
3. WHILE data is being fetched, THE Reference_Data_Page SHALL display a SkeletonLoader.
4. IF a data fetch request fails, THEN THE Audit_Logs_Page SHALL display an error toast notification.
5. IF a data fetch request fails, THEN THE Error_Logs_Page SHALL display an error toast notification.
6. IF a data fetch request fails, THEN THE Reference_Data_Page SHALL display an error toast notification.

### Requirement 16: Form Error Handling — Create Operations

**User Story:** As an OrgAdmin, I want to see clear error feedback when creating reference data entries fails, so that I can correct issues and retry.

#### Acceptance Criteria

1. IF a create department type request fails with an ApiError, THEN THE Reference_Data_Page SHALL display an error toast with the mapped error code message.
2. IF a create priority level request fails with an ApiError, THEN THE Reference_Data_Page SHALL display an error toast with the mapped error code message.
3. WHILE a create request is in progress, THE Reference_Data_Page SHALL disable the submit button and show a loading indicator.
