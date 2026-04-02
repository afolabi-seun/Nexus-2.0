# Implementation Plan: Admin Frontend Features

## Overview

Add four feature areas to the Nexus 2.0 React/TypeScript frontend: Audit Logs Page, Error Logs Page, Reference Data Page, and Sprint Edit Modal. Implementation proceeds bottom-up: types and API clients first, then page components, then router wiring, then a build checkpoint.

## Tasks

- [x] 1. Add new types and API client methods
  - [x] 1.1 Add new types to `src/frontend/src/types/utility.ts`
    - Add `ErrorLog` interface with fields: errorLogId, serviceName, errorCode, severity, message, stackTrace (string | null), dateCreated
    - Add `ErrorLogFilters` interface with optional fields: serviceName, errorCode, severity, dateFrom, dateTo
    - Add `CreateDepartmentTypeRequest` interface with fields: code, name
    - Add `CreatePriorityLevelRequest` interface with fields: code, name, level (number)
    - _Requirements: 5.2, 5.5, 7.1, 10.2, 10.5, 10.7_

  - [x] 1.2 Add `UpdateSprintRequest` type to `src/frontend/src/types/work.ts`
    - Add `UpdateSprintRequest` interface with fields: name, goal (optional), startDate, endDate
    - _Requirements: 14.4, 14.7_

  - [x] 1.3 Add 8 new methods to `src/frontend/src/api/utilityApi.ts`
    - Add `getArchivedAuditLogs` calling `GET /api/v1/audit-logs/archive` with filter + pagination params, returning `PaginatedResponse<AuditLog>`
    - Add `getErrorLogs` calling `GET /api/v1/error-logs` with `ErrorLogFilters` + pagination params, returning `PaginatedResponse<ErrorLog>`
    - Add `getDepartmentTypes` calling `GET /api/v1/reference/department-types`, returning `DepartmentType[]`
    - Add `getPriorityLevels` calling `GET /api/v1/reference/priority-levels`, returning `PriorityLevel[]`
    - Add `getTaskTypes` calling `GET /api/v1/reference/task-types`, returning `TaskTypeRef[]`
    - Add `getWorkflowStates` calling `GET /api/v1/reference/workflow-states`, returning `WorkflowState[]`
    - Add `createDepartmentType` calling `POST /api/v1/reference/department-types` with `CreateDepartmentTypeRequest`
    - Add `createPriorityLevel` calling `POST /api/v1/reference/priority-levels` with `CreatePriorityLevelRequest`
    - Import the new types (`ErrorLog`, `ErrorLogFilters`, `CreateDepartmentTypeRequest`, `CreatePriorityLevelRequest`) from `@/types/utility`
    - _Requirements: 3.4, 5.5, 9.3, 9.4, 9.5, 9.6, 9.7, 10.7_

  - [x] 1.4 Add `updateSprint` method to `src/frontend/src/api/workApi.ts`
    - Add `updateSprint` calling `PUT /api/v1/sprints/{id}` with `UpdateSprintRequest`, returning `SprintDetail`
    - Import `UpdateSprintRequest` from `@/types/work`
    - _Requirements: 14.7_

- [ ] 2. Implement AuditLogsPage
  - [x] 2.1 Create `src/frontend/src/features/admin/pages/AuditLogsPage.tsx`
    - Implement state: logs, loading, page (default 1), pageSize (default 20), totalCount, isArchive (boolean), and filter state (filterAction, filterEntityType, filterActorId, filterDateFrom, filterDateTo)
    - Implement `fetchLogs` callback that builds params from filters + pagination and calls `utilityApi.getAuditLogs()` or `utilityApi.getArchivedAuditLogs()` based on `isArchive`
    - Render DataTable with columns: action, entityType, entityId, actorName, ipAddress, details (truncated), dateCreated (formatted)
    - Render filter controls: action dropdown, entityType dropdown, actorId text input, dateFrom date input, dateTo date input
    - Render live/archive toggle (button group); toggling resets page to 1, preserves filters
    - Filter changes reset page to 1 and re-fetch
    - Render Pagination component with configurable page size (10, 20, 50)
    - Show `SkeletonLoader variant="table"` while loading
    - Error handling: try/catch with `addToast('error', ...)` on fetch failure
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 15.1, 15.4_

  - [ ]* 2.2 Write property test: filter changes reset pagination
    - **Property 3: Filter changes reset pagination to page 1**
    - Use `fast-check` to generate random page numbers > 1 and filter values; simulate filter change; assert page resets to 1
    - **Validates: Requirements 2.2, 7.2**

  - [ ]* 2.3 Write property test: archive toggle preserves filters and resets page
    - **Property 4: Archive toggle preserves filters and resets page**
    - Use `fast-check` to generate random filter states and page numbers; toggle archive; assert filters unchanged and page = 1
    - **Validates: Requirements 3.3**

- [ ] 3. Implement ErrorLogsPage
  - [x] 3.1 Create `src/frontend/src/features/admin/pages/ErrorLogsPage.tsx`
    - Implement state: logs, loading, page, pageSize, totalCount, expandedRows (Set<string>), and filter state (filterServiceName, filterErrorCode, filterSeverity, filterDateFrom, filterDateTo)
    - Implement `fetchLogs` callback calling `utilityApi.getErrorLogs()` with filter + pagination params
    - Render DataTable with columns: serviceName, errorCode, severity (via Badge with color mapping: Critical→red, Error→orange, Warning→yellow, Info→blue), message, stackTrace (truncated with expand toggle), dateCreated
    - Implement expand/collapse: clicking chevron toggles row ID in `expandedRows` Set; expanded rows show `<pre>` block with full stack trace; rows with null stackTrace have no toggle
    - Render filter controls: serviceName dropdown, errorCode text input, severity dropdown, dateFrom date input, dateTo date input
    - Filter changes reset page to 1 and re-fetch
    - Render Pagination component with configurable page size (10, 20, 50)
    - Show `SkeletonLoader variant="table"` while loading
    - Error handling: try/catch with `addToast('error', ...)` on fetch failure
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3, 15.2, 15.5_

  - [ ]* 3.2 Write property test: severity badge renders correct color
    - **Property 5: Severity badge renders correct color per level**
    - Use `fast-check` to generate severity values from {Critical, Error, Warning, Info}; render Badge; assert correct color class
    - **Validates: Requirements 5.3**

  - [ ]* 3.3 Write property test: stack trace expand/collapse round-trip
    - **Property 6: Stack trace expand/collapse round-trip**
    - Use `fast-check` to generate ErrorLog objects with/without stackTrace; test expand/collapse behavior and toggle visibility
    - **Validates: Requirements 6.1, 6.2, 6.3**

- [x] 4. Checkpoint — Verify audit and error log pages
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Implement ReferenceDataPage
  - [x] 5.1 Create `src/frontend/src/features/admin/pages/ReferenceDataPage.tsx`
    - Implement state: activeTab ('departmentTypes' | 'priorityLevels' | 'taskTypes' | 'workflowStates'), data arrays for each type, loading, createModalOpen, saving, form state (code, name, level), errors
    - On mount, fetch all four reference data lists via `utilityApi.getDepartmentTypes()`, `utilityApi.getPriorityLevels()`, `utilityApi.getTaskTypes()`, `utilityApi.getWorkflowStates()`
    - Render tab bar with four tabs; clicking a tab sets `activeTab`
    - Department Types tab: DataTable with code/name columns + "Add Department Type" button → opens Modal with code + name fields
    - Priority Levels tab: DataTable with code/name/level columns + "Add Priority Level" button → opens Modal with code + name + level fields
    - Task Types tab: read-only DataTable with code/name/defaultDepartment columns, no create button
    - Workflow States tab: read-only DataTable with entityType/status/validTransitions columns, no create button
    - Create forms validate required fields; POST via `utilityApi.createDepartmentType()` or `utilityApi.createPriorityLevel()`; refresh list on success; show success toast
    - Error handling: ApiError → `mapErrorCode` → error toast; disable submit button + loading indicator while saving
    - Show `SkeletonLoader variant="table"` while loading
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 11.1, 11.2, 15.3, 15.6, 16.1, 16.2, 16.3_

  - [ ]* 5.2 Write property test: tab selection displays correct reference data
    - **Property 7: Tab selection displays correct reference data**
    - Use `fast-check` to generate random tab selections; assert correct data displayed in DataTable
    - **Validates: Requirements 9.2**

- [ ] 6. Implement Sprint Edit on SprintDetailPage
  - [x] 6.1 Add SprintEditModal and edit button to `src/frontend/src/features/sprints/pages/SprintDetailPage.tsx`
    - Add `editOpen` state to SprintDetailPage
    - Use `useAuth()` to get `user.roleName`; show "Edit" button (Pencil icon) only when role is `OrgAdmin` or `DeptLead`; hide for `Member`/`Viewer`
    - Implement `SprintEditModal` sub-component with props: open, onClose, sprint, onUpdated
    - Pre-populate form with current sprint name, goal, startDate, endDate
    - Validate: name non-empty, startDate < endDate; show inline validation errors via FormField error prop
    - On valid submit: call `workApi.updateSprint(id, payload)` with UpdateSprintRequest; on success: close modal, show success toast, call `onUpdated()` to refresh
    - On error: ApiError → `mapErrorCode` → error toast; generic error → fallback toast
    - Disable submit button + show "Saving..." while request in progress
    - _Requirements: 13.1, 13.2, 13.3, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6_

  - [ ]* 6.2 Write property test: sprint edit button visibility by role
    - **Property 9: Sprint edit button visibility by role**
    - Use `fast-check` to generate random role names; render SprintDetailPage; assert Edit button visible iff role is OrgAdmin or DeptLead
    - **Validates: Requirements 13.1, 13.3**

  - [ ]* 6.3 Write property test: sprint edit validation rejects invalid input
    - **Property 11: Sprint edit validation rejects invalid input**
    - Use `fast-check` to generate invalid inputs (empty names, startDate >= endDate); assert validation errors shown and no API call made
    - **Validates: Requirements 14.1, 14.2, 14.3**

- [x] 7. Update router with all new routes
  - [x] 7.1 Add new routes to `src/frontend/src/router.tsx`
    - Import `AuditLogsPage`, `ErrorLogsPage`, `ReferenceDataPage` from `@/features/admin/pages/`
    - Add `{ path: '/audit-logs', element: <AuditLogsPage /> }` to the OrgAdmin-only RoleGuard children block
    - Add `{ path: '/settings/error-logs', element: <ErrorLogsPage /> }` to the OrgAdmin-only RoleGuard children block
    - Add `{ path: '/settings/reference-data', element: <ReferenceDataPage /> }` to the OrgAdmin-only RoleGuard children block
    - Add `{ path: '/admin/audit-logs', element: <AuditLogsPage /> }` to the PlatformAdmin AdminLayout children block
    - _Requirements: 4.1, 4.2, 4.3, 8.1, 8.2, 12.1, 12.2_

- [x] 8. Final checkpoint — Build and verify
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests use `fast-check` and validate universal correctness properties from the design document
- All new pages follow existing codebase patterns: useState/useEffect/useCallback hooks, DataTable/Pagination/Modal/Badge/SkeletonLoader shared components, useToast for notifications, ApiError + mapErrorCode for error handling
