# Requirements Document — Frontend Application

## Introduction

This document defines the complete requirements for the Nexus-2.0 Frontend Application — a React + TypeScript + Vite single-page application (SPA) that serves as the user interface for the entire Enterprise Agile Platform. The frontend consumes all 4 backend microservices (SecurityService on port 5001, ProfileService on port 5002, WorkService on port 5003, UtilityService on port 5200) via their REST APIs.

The frontend provides the complete user experience: authentication flows, dashboard, project management, story and task management with workflow state machines, sprint planning and metrics, board views (Kanban, Sprint, Department, Backlog) with drag-and-drop, threaded comments with @mentions, team member management, organization settings, user preferences, invitation system, device/session management, notification preferences, global search, reports, and saved filters.

All requirements are derived from:
- `docs/nexus-2.0-backend-requirements.md` (Appendix D — Frontend Application)
- `.kiro/specs/security-service/requirements.md` (SecurityService endpoints and auth flows)
- `.kiro/specs/profile-service/requirements.md` (ProfileService endpoints and data models)
- `.kiro/specs/work-service/requirements.md` (WorkService endpoints and Agile workflows)
- `.kiro/specs/utility-service/requirements.md` (UtilityService endpoints and reference data)

## Glossary

- **Frontend_App**: React 18+ / TypeScript / Vite single-page application consuming all 4 backend microservices. Runs on a configurable port (default 5173 in dev).
- **SecurityService**: Backend microservice (port 5001) providing authentication, JWT issuance, session management, OTP, password management, and RBAC.
- **ProfileService**: Backend microservice (port 5002) providing organization management, department management, team member profiles, invitations, devices, notification settings, user preferences, department preferences, and PlatformAdmin management.
- **WorkService**: Backend microservice (port 5003) providing project management, story CRUD, task CRUD, sprint management, board views, comments, labels, search, reports, saved filters, and workflow customization.
- **UtilityService**: Backend microservice (port 5200) providing audit logs, error logs, notification logs, error code registry, and reference data (department types, priority levels, task types, workflow states).
- **API_Client**: Typed Axios instance configured per backend service with base URL, JWT interceptor, error handling, and response parsing.
- **JWT**: JSON Web Token used for Bearer authentication. Access token (short-lived, ~15 min) stored in memory; refresh token stored securely.
- **Access_Token**: Short-lived JWT attached to every API request via Axios interceptor.
- **Refresh_Token**: Long-lived token used to obtain new access/refresh token pairs when the access token expires.
- **ApiResponse**: Standardized JSON envelope from all backend services: `{ ResponseCode, Success, Data, ErrorCode, CorrelationId, Errors }`.
- **Zustand**: Lightweight state management library used for global stores (auth, organization, theme).
- **React_Router**: Client-side routing library providing route definitions, auth guards, and navigation.
- **Auth_Guard**: Route protection component that redirects unauthenticated users to the login page.
- **Role_Guard**: Route protection component that restricts access based on the user's role (OrgAdmin, DeptLead, Member, Viewer).
- **Shadcn_UI**: Component library built on Radix UI primitives providing accessible, composable UI components.
- **React_Hook_Form**: Form library for performant form state management with Zod schema validation.
- **Zod**: TypeScript-first schema validation library used for form validation, mirroring backend FluentValidation rules.
- **dnd_kit**: Drag-and-drop library used for Kanban board column reordering and sprint planning.
- **Recharts**: Charting library used for burndown charts, velocity charts, and report visualizations.
- **Toast**: Non-blocking notification component for displaying API success/error messages to the user.
- **Story_Key**: Human-readable professional ID for stories in format `{ProjectKey}-{SequenceNumber}` (e.g., `NEXUS-42`).
- **Workflow_State_Machine**: Frontend representation of valid status transitions for stories (Backlog→Ready→InProgress→InReview→QA→Done→Closed) and tasks (ToDo→InProgress→InReview→Done).
- **Board**: Structured view of work items — Kanban (stories by status), Sprint Board (tasks by status), Department Board (tasks by department), Backlog (unassigned stories by priority).
- **Saved_Filter**: User-saved filter configuration for board views and search, stored as JSON via WorkService.
- **Preference_Cascade**: Resolution order for user preferences: User → Department → Organization → System Default.
- **Organization**: Top-level tenant entity. All frontend data is scoped to the authenticated user's organization.
- **Department**: Functional unit within an organization (Engineering, QA, DevOps, Product, Design, plus custom).
- **Role**: Department-scoped permission level — OrgAdmin (100), DeptLead (75), Member (50), Viewer (25).
- **PlatformAdmin**: Platform-level administrator above organization scope. Separate login flow, no organization context.
- **Sprint**: Time-boxed iteration (1–4 weeks) with lifecycle: Planning → Active → Completed/Cancelled.
- **Story_Points**: Fibonacci-scale estimation (1, 2, 3, 5, 8, 13, 21).
- **Priority**: Urgency level — Critical, High, Medium, Low.
- **Task_Type**: Classification determining department routing — Development, Testing, DevOps, Design, Documentation, Bug.
- **OTP**: One-Time Password — 6-digit numeric code with 5-minute TTL for sensitive operations.
- **FlgStatus**: Soft-delete lifecycle field — `A` (Active), `S` (Suspended), `D` (Deactivated).

## Requirements

### Requirement 1: API Client Layer

**User Story:** As a developer, I want typed Axios API clients per backend service so that all HTTP communication is centralized, type-safe, and handles authentication automatically.

#### Acceptance Criteria

1. THE Frontend_App SHALL create four typed Axios instances — `securityApi`, `profileApi`, `workApi`, `utilityApi` — each configured with the corresponding backend service base URL from environment variables (`VITE_SECURITY_API_URL`, `VITE_PROFILE_API_URL`, `VITE_WORK_API_URL`, `VITE_UTILITY_API_URL`).
2. THE Frontend_App SHALL attach an Axios request interceptor to all API clients that adds the `Authorization: Bearer {accessToken}` header from the auth store to every outgoing request.
3. THE Frontend_App SHALL attach an Axios response interceptor that detects HTTP 401 responses, attempts a token refresh via `POST /api/v1/auth/refresh`, retries the original request with the new access token, and redirects to the login page if the refresh fails.
4. THE Frontend_App SHALL parse all API responses as `ApiResponse<T>` and extract the `Data` field for successful responses or throw a typed error with `ErrorCode`, `Message`, and `Errors` for failed responses.
5. WHEN the Axios response interceptor detects a refresh token reuse (HTTP 401 with `REFRESH_TOKEN_REUSE`), THE Frontend_App SHALL clear all auth state, redirect to the login page, and display a session-expired toast.
6. THE Frontend_App SHALL include the `X-Correlation-Id` header (generated as a UUID) on every outgoing request for end-to-end tracing.
7. THE Frontend_App SHALL export typed API functions per service that return strongly-typed TypeScript responses matching the backend DTOs.

### Requirement 2: Authentication — Login

**User Story:** As a team member, I want to log in with my email and password so that I can access the platform.

#### Acceptance Criteria

1. WHEN the user navigates to `/login`, THE Frontend_App SHALL render a login form with email and password fields, a "Login" submit button, and a "Forgot Password?" link.
2. WHEN the user submits valid credentials, THE Frontend_App SHALL call `POST /api/v1/auth/login` on SecurityService and store the `accessToken` in memory (Zustand auth store) and the `refreshToken` in a secure location.
3. WHEN login succeeds and `isFirstTimeUser` is `true`, THE Frontend_App SHALL redirect to `/password/change` for forced password change.
4. WHEN login succeeds and `isFirstTimeUser` is `false`, THE Frontend_App SHALL redirect to the dashboard (`/`).
5. WHEN login fails with `INVALID_CREDENTIALS` (2001), THE Frontend_App SHALL display an inline error message "Invalid email or password" without revealing which field is incorrect.
6. WHEN login fails with `ACCOUNT_LOCKED` (2002), THE Frontend_App SHALL display "Account locked. Try again later." with the lockout duration.
7. WHEN login fails with `ACCOUNT_INACTIVE` (2003), THE Frontend_App SHALL display "Account is inactive. Contact your administrator."
8. WHEN login fails with `SUSPICIOUS_LOGIN` (2017), THE Frontend_App SHALL display "Login blocked due to suspicious activity. Check your email for verification."
9. THE Frontend_App SHALL validate the login form client-side: email must be a valid email format, password must not be empty.

### Requirement 3: Authentication — PlatformAdmin Login

**User Story:** As a PlatformAdmin, I want to log in with my username and password so that I can manage organizations.

#### Acceptance Criteria

1. WHEN the user navigates to `/admin/login`, THE Frontend_App SHALL render a login form with username and password fields and a "Login" submit button.
2. WHEN the PlatformAdmin submits valid credentials, THE Frontend_App SHALL call `POST /api/v1/auth/login` on SecurityService with the username (instead of email) and store the tokens.
3. WHEN PlatformAdmin login succeeds, THE Frontend_App SHALL redirect to `/admin/organizations` (PlatformAdmin dashboard).
4. WHEN PlatformAdmin login succeeds and `isFirstTimeUser` is `true`, THE Frontend_App SHALL redirect to `/password/change` for forced password change.
5. THE Frontend_App SHALL detect PlatformAdmin context from JWT claims (no `organizationId`, `roleName=PlatformAdmin`) and render the PlatformAdmin layout instead of the standard organization layout.

### Requirement 4: Authentication — Forced Password Change

**User Story:** As a new team member, I want to change my temporary password on first login so that my account is secured.

#### Acceptance Criteria

1. WHEN the user navigates to `/password/change` with `isFirstTimeUser=true`, THE Frontend_App SHALL render a form with "New Password" and "Confirm Password" fields.
2. WHEN the user submits a valid new password, THE Frontend_App SHALL call `POST /api/v1/password/forced-change` on SecurityService.
3. WHEN the password change succeeds, THE Frontend_App SHALL redirect to the dashboard (`/`).
4. WHEN the new password fails complexity validation (`PASSWORD_COMPLEXITY_FAILED` 2018), THE Frontend_App SHALL display the specific complexity requirements: minimum 8 characters, 1 uppercase, 1 lowercase, 1 digit, 1 special character from `!@#$%^&*`.
5. WHEN the new password matches the temporary password (`PASSWORD_REUSE_NOT_ALLOWED` 2004), THE Frontend_App SHALL display "New password cannot be the same as the temporary password."
6. THE Frontend_App SHALL validate password complexity client-side before submission using a Zod schema matching the backend rules.
7. WHILE `isFirstTimeUser=true`, WHEN the user attempts to navigate to any page other than `/password/change`, THE Frontend_App SHALL redirect back to `/password/change`.

### Requirement 5: Authentication — Password Reset

**User Story:** As a team member, I want to reset my forgotten password via OTP so that I can regain access to my account.

#### Acceptance Criteria

1. WHEN the user clicks "Forgot Password?" on the login page, THE Frontend_App SHALL navigate to `/password/reset` and render an email input form.
2. WHEN the user submits their email, THE Frontend_App SHALL call `POST /api/v1/password/reset/request` on SecurityService and display an OTP input form.
3. WHEN the user enters the 6-digit OTP and a new password, THE Frontend_App SHALL call `POST /api/v1/password/reset/confirm` on SecurityService.
4. WHEN the password reset succeeds, THE Frontend_App SHALL redirect to `/login` with a success toast "Password reset successfully. Please log in."
5. WHEN the OTP is expired (`OTP_EXPIRED` 2007), THE Frontend_App SHALL display "Code expired. Request a new one." with a "Resend Code" button.
6. WHEN the OTP max attempts are reached (`OTP_MAX_ATTEMPTS` 2009), THE Frontend_App SHALL display "Too many attempts. Request a new code."
7. WHEN the new password was recently used (`PASSWORD_RECENTLY_USED` 2005), THE Frontend_App SHALL display "This password was recently used. Choose a different one."
8. THE Frontend_App SHALL validate the OTP field as exactly 6 digits and the new password against complexity rules client-side.

### Requirement 6: Authentication — Session Management and Logout

**User Story:** As a team member, I want to manage my sessions and log out so that I can control access to my account.

#### Acceptance Criteria

1. WHEN the user clicks "Logout" in the header/sidebar, THE Frontend_App SHALL call `POST /api/v1/auth/logout` on SecurityService, clear the auth store (access token, refresh token, user data), and redirect to `/login`.
2. WHEN the Axios interceptor receives HTTP 401 and the refresh token is invalid or expired, THE Frontend_App SHALL clear the auth store and redirect to `/login` with a toast "Session expired. Please log in again."
3. THE Frontend_App SHALL store the access token only in the Zustand auth store (in-memory) and not in localStorage or sessionStorage.
4. WHEN the application loads, THE Frontend_App SHALL attempt to restore the session by calling the refresh endpoint if a refresh token exists.

### Requirement 7: Authentication — State Management (Auth Store)

**User Story:** As a developer, I want a centralized auth store so that authentication state is accessible throughout the application.

#### Acceptance Criteria

1. THE Frontend_App SHALL create a Zustand auth store containing: `accessToken` (string | null), `refreshToken` (string | null), `user` (object with `userId`, `organizationId`, `departmentId`, `roleName`, `email`, `firstName`, `lastName`, `isFirstTimeUser`), `isAuthenticated` (boolean), and `isPlatformAdmin` (boolean).
2. THE Frontend_App SHALL provide auth store actions: `login(tokens, user)`, `logout()`, `refreshTokens(newAccessToken, newRefreshToken)`, `setUser(user)`.
3. WHEN the auth store's `accessToken` is set, THE Frontend_App SHALL decode the JWT payload to extract user claims and populate the `user` object.
4. THE Frontend_App SHALL persist the refresh token using a secure mechanism (httpOnly cookie preferred, or encrypted localStorage as fallback) so that sessions survive page refreshes.

### Requirement 8: Routing and Navigation

**User Story:** As a user, I want clear navigation with proper auth guards so that I can access all platform features and am redirected when unauthorized.

#### Acceptance Criteria

1. THE Frontend_App SHALL define the following routes using React Router:

| Route | Auth | Role Guard | Component |
|-------|------|------------|-----------|
| `/login` | No | None | LoginPage |
| `/admin/login` | No | None | PlatformAdminLoginPage |
| `/password/change` | Yes (first-time) | None | ForcedPasswordChangePage |
| `/password/reset` | No | None | PasswordResetPage |
| `/invites/:token` | No | None | AcceptInvitePage |
| `/` | Yes | Any | DashboardPage |
| `/projects` | Yes | Any | ProjectListPage |
| `/projects/:id` | Yes | Any | ProjectDetailPage |
| `/stories` | Yes | Any | StoryListPage |
| `/stories/:id` | Yes | Any | StoryDetailPage |
| `/stories/key/:key` | Yes | Any | StoryByKeyRedirect |
| `/boards/kanban` | Yes | Any | KanbanBoardPage |
| `/boards/sprint` | Yes | Any | SprintBoardPage |
| `/boards/department` | Yes | Any | DepartmentBoardPage |
| `/boards/backlog` | Yes | Any | BacklogPage |
| `/sprints` | Yes | Any | SprintListPage |
| `/sprints/:id` | Yes | Any | SprintDetailPage |
| `/members` | Yes | Any | MemberListPage |
| `/members/:id` | Yes | Any | MemberProfilePage |
| `/departments` | Yes | Any | DepartmentListPage |
| `/departments/:id` | Yes | Any | DepartmentDetailPage |
| `/settings` | Yes | OrgAdmin | SettingsPage |
| `/preferences` | Yes | Any | PreferencesPage |
| `/invites` | Yes | OrgAdmin, DeptLead | InviteManagementPage |
| `/sessions` | Yes | Any | SessionManagementPage |
| `/search` | Yes | Any | SearchPage |
| `/reports` | Yes | Any | ReportsPage |
| `/admin/organizations` | Yes | PlatformAdmin | PlatformAdminOrganizationsPage |
| `*` | — | — | NotFoundPage |

2. WHEN an unauthenticated user navigates to a protected route, THE Frontend_App SHALL redirect to `/login` and store the intended destination for post-login redirect.
3. WHEN an authenticated user navigates to a role-restricted route without the required role, THE Frontend_App SHALL redirect to `/` and display a toast "You don't have permission to access this page."
4. WHEN an authenticated user navigates to `/login`, THE Frontend_App SHALL redirect to `/`.
5. THE Frontend_App SHALL render a persistent layout (sidebar, header) for all authenticated routes and a minimal layout (centered card) for unauthenticated routes.
6. WHEN the user navigates to `/stories/key/:key` (e.g., `/stories/key/NEXUS-42`), THE Frontend_App SHALL call `GET /api/v1/stories/by-key/{key}` on WorkService and redirect to `/stories/:id` with the resolved story ID.

### Requirement 9: Application Shell and Layout

**User Story:** As a user, I want a consistent application layout with sidebar navigation so that I can navigate the platform efficiently.

#### Acceptance Criteria

1. THE Frontend_App SHALL render an application shell with: a collapsible sidebar (left), a top header bar, and a main content area.
2. THE Frontend_App SHALL include the following sidebar navigation items: Dashboard, Projects, Stories, Boards (expandable: Kanban, Sprint Board, Department Board, Backlog), Sprints, Members, Departments, Reports, Search.
3. THE Frontend_App SHALL include the following header elements: organization name, user avatar with dropdown menu (Profile, Preferences, Sessions, Logout), notification bell icon, and a global search input.
4. WHEN the user's role is OrgAdmin, THE Frontend_App SHALL show additional sidebar items: Settings, Invites.
5. WHEN the user is a PlatformAdmin, THE Frontend_App SHALL render a separate admin layout with sidebar items: Organizations, and a header with admin-specific controls.
6. THE Frontend_App SHALL highlight the active sidebar item based on the current route.
7. THE Frontend_App SHALL support collapsing the sidebar to icon-only mode and persist the collapsed state in user preferences.

### Requirement 10: Dashboard

**User Story:** As a team member, I want a home dashboard with key widgets so that I can see my work status at a glance.

#### Acceptance Criteria

1. WHEN the user navigates to `/`, THE Frontend_App SHALL render the dashboard with the following widgets: Sprint Progress, My Tasks, Recent Activity, and Velocity Chart.
2. WHEN the Sprint Progress widget loads, THE Frontend_App SHALL call `GET /api/v1/sprints/active` on WorkService and display the active sprint's name, progress bar (completed stories / total stories), remaining days, and total story points.
3. WHEN the My Tasks widget loads, THE Frontend_App SHALL call `GET /api/v1/stories?assignee={userId}&status=InProgress,InReview` on WorkService and display the user's assigned stories and tasks with status badges.
4. WHEN the Recent Activity widget loads, THE Frontend_App SHALL call `GET /api/v1/audit-logs?action=StoryCreated,TaskAssigned,StatusChanged&pageSize=20` on UtilityService and display a chronological feed of recent Agile events.
5. WHEN the Velocity Chart widget loads, THE Frontend_App SHALL call `GET /api/v1/sprints/velocity?count=10` on WorkService and render a bar chart using Recharts showing velocity per sprint.
6. IF any widget fails to load, THEN THE Frontend_App SHALL display a graceful error state within that widget without affecting other widgets.
7. THE Frontend_App SHALL load all dashboard widgets in parallel to minimize page load time.

### Requirement 11: Project Management

**User Story:** As an OrgAdmin or DeptLead, I want to create and manage projects so that work is organized into separate backlogs.

#### Acceptance Criteria

1. WHEN the user navigates to `/projects`, THE Frontend_App SHALL call `GET /api/v1/projects` on WorkService and display a paginated list of projects with name, project key, story count, sprint count, lead name, and status badge.
2. WHEN the user clicks "Create Project" (visible to OrgAdmin and DeptLead), THE Frontend_App SHALL open a form with fields: Project Name, Project Key (2–10 uppercase alphanumeric), Description, and Lead (team member selector).
3. WHEN the create project form is submitted, THE Frontend_App SHALL call `POST /api/v1/projects` on WorkService.
4. WHEN `PROJECT_KEY_DUPLICATE` (4043) is returned, THE Frontend_App SHALL display "This project key is already in use."
5. WHEN `PROJECT_NAME_DUPLICATE` (4042) is returned, THE Frontend_App SHALL display "A project with this name already exists."
6. WHEN the user clicks a project row, THE Frontend_App SHALL navigate to `/projects/:id` and display the project detail with story list, sprint list, and project settings.
7. THE Frontend_App SHALL validate the Project Key field client-side with a Zod schema matching `^[A-Z0-9]{2,10}$`.

### Requirement 12: Story List and Filtering

**User Story:** As a team member, I want to view and filter stories so that I can find relevant work items quickly.

#### Acceptance Criteria

1. WHEN the user navigates to `/stories`, THE Frontend_App SHALL call `GET /api/v1/stories` on WorkService and display a paginated table of stories with columns: Story Key, Title, Status, Priority, Story Points, Assignee, Sprint, Project, and Labels.
2. THE Frontend_App SHALL provide filter controls for: project (dropdown), status (multi-select), priority (multi-select), department (dropdown), assignee (searchable dropdown), sprint (dropdown), labels (multi-select), and date range (date picker).
3. WHEN any filter is changed, THE Frontend_App SHALL update the query parameters and re-fetch the story list.
4. THE Frontend_App SHALL support sorting by any column (ascending/descending) via clickable column headers.
5. THE Frontend_App SHALL display pagination controls with page number, page size selector (10, 20, 50), and total count.
6. WHEN the user clicks a story row, THE Frontend_App SHALL navigate to `/stories/:id`.
7. THE Frontend_App SHALL render status badges with color coding: Backlog (gray), Ready (blue), InProgress (yellow), InReview (purple), QA (orange), Done (green), Closed (dark gray).
8. THE Frontend_App SHALL render priority badges with color coding: Critical (red), High (orange), Medium (yellow), Low (green).

### Requirement 13: Story Detail

**User Story:** As a team member, I want a comprehensive story detail view so that I can see all information about a story in one place.

#### Acceptance Criteria

1. WHEN the user navigates to `/stories/:id`, THE Frontend_App SHALL call `GET /api/v1/stories/{id}` on WorkService and display the full story detail.
2. THE Frontend_App SHALL display the story header with: Story Key (e.g., `NEXUS-42`), Title, Status badge, Priority badge, Story Points, Assignee avatar and name, Reporter name, Project name, Sprint name, Due Date, and Created/Updated dates.
3. THE Frontend_App SHALL display a Description section with Markdown rendering.
4. THE Frontend_App SHALL display an Acceptance Criteria section with Markdown rendering.
5. THE Frontend_App SHALL display a Tasks section listing all tasks with: Title, Status, Task Type, Assignee, Department, Estimated Hours, Actual Hours. Include a "Create Task" button (visible to OrgAdmin, DeptLead, Member).
6. THE Frontend_App SHALL display a completion percentage bar based on `CompletedTasks / TotalTasks * 100` and a department contribution breakdown showing which departments have tasks and their completion status.
7. THE Frontend_App SHALL display a Comments section with threaded comments, a comment input with Markdown support and @mention autocomplete.
8. THE Frontend_App SHALL display a Labels section showing applied labels as colored chips with an "Add Label" button.
9. THE Frontend_App SHALL display an Activity Log section showing a chronological timeline of all changes (status transitions, assignments, edits, comments, label changes).
10. THE Frontend_App SHALL display a Links section showing related stories with link type (blocks, is_blocked_by, relates_to, duplicates) and a "Link Story" button.
11. THE Frontend_App SHALL display workflow transition buttons based on the current status and valid transitions from the state machine. WHEN a transition button is clicked, THE Frontend_App SHALL call `PATCH /api/v1/stories/{id}/status` on WorkService.
12. WHEN a transition fails with a condition error (e.g., `STORY_REQUIRES_ASSIGNEE` 4013, `STORY_REQUIRES_TASKS` 4014, `STORY_REQUIRES_POINTS` 4015), THE Frontend_App SHALL display a toast with the specific missing requirement.
13. THE Frontend_App SHALL display an "Assign" button (visible to DeptLead+) that opens a team member selector. WHEN a member is selected, THE Frontend_App SHALL call `PATCH /api/v1/stories/{id}/assign` on WorkService.

### Requirement 14: Story Create and Edit

**User Story:** As a team member, I want to create and edit stories so that I can manage the product backlog.

#### Acceptance Criteria

1. WHEN the user clicks "Create Story", THE Frontend_App SHALL open a form (modal or page) with fields: Project (dropdown, required), Title (required, max 200), Description (Markdown editor, max 5000), Acceptance Criteria (Markdown editor, max 5000), Priority (dropdown: Critical/High/Medium/Low), Story Points (dropdown: 1/2/3/5/8/13/21), Department (dropdown), Due Date (date picker), and Labels (multi-select).
2. WHEN the create form is submitted, THE Frontend_App SHALL call `POST /api/v1/stories` on WorkService.
3. WHEN story creation succeeds, THE Frontend_App SHALL navigate to the new story's detail page and display a success toast.
4. WHEN the user clicks "Edit" on a story detail page, THE Frontend_App SHALL populate the form with existing values and call `PUT /api/v1/stories/{id}` on submission.
5. THE Frontend_App SHALL validate the form client-side using React Hook Form + Zod: Title required and max 200 chars, Description max 5000 chars, Story Points must be Fibonacci (1,2,3,5,8,13,21), Priority must be one of the 4 values.
6. WHEN `INVALID_STORY_POINTS` (4023) is returned, THE Frontend_App SHALL display "Story points must be a Fibonacci number (1, 2, 3, 5, 8, 13, 21)."
7. WHEN `INVALID_PRIORITY` (4024) is returned, THE Frontend_App SHALL display "Invalid priority value."

### Requirement 15: Story Labels

**User Story:** As a team member, I want to manage labels on stories so that I can categorize and filter work items.

#### Acceptance Criteria

1. WHEN the user clicks "Add Label" on a story detail page, THE Frontend_App SHALL display a dropdown of available labels (from `GET /api/v1/labels` on WorkService) with a search filter.
2. WHEN a label is selected, THE Frontend_App SHALL call `POST /api/v1/stories/{id}/labels` on WorkService.
3. WHEN the user clicks the "x" on a label chip, THE Frontend_App SHALL call `DELETE /api/v1/stories/{id}/labels/{labelId}` on WorkService.
4. WHEN the user clicks "Manage Labels" (visible to DeptLead+), THE Frontend_App SHALL open a label management dialog with create (name + color picker), edit, and delete actions calling the corresponding WorkService label endpoints.
5. WHEN `LABEL_NAME_DUPLICATE` (4011) is returned, THE Frontend_App SHALL display "A label with this name already exists."

### Requirement 16: Story Linking

**User Story:** As a team member, I want to link related stories so that dependencies are visible.

#### Acceptance Criteria

1. WHEN the user clicks "Link Story" on a story detail page, THE Frontend_App SHALL open a dialog with a story search field (by key or title) and a link type dropdown (blocks, is blocked by, relates to, duplicates).
2. WHEN the link form is submitted, THE Frontend_App SHALL call `POST /api/v1/stories/{id}/links` on WorkService.
3. THE Frontend_App SHALL display linked stories in the Links section with: Story Key (clickable link to detail), Title, Link Type badge, and a remove button.
4. WHEN the remove button is clicked, THE Frontend_App SHALL call `DELETE /api/v1/stories/{id}/links/{linkId}` on WorkService.

### Requirement 17: Task Management

**User Story:** As a team member, I want to create and manage tasks within stories so that work is broken down into actionable items.

#### Acceptance Criteria

1. WHEN the user clicks "Create Task" on a story detail page, THE Frontend_App SHALL open a form with fields: Title (required, max 200), Description (max 3000), Task Type (dropdown: Development/Testing/DevOps/Design/Documentation/Bug), Priority (dropdown), Estimated Hours (number), and Due Date (date picker).
2. WHEN the create task form is submitted, THE Frontend_App SHALL call `POST /api/v1/tasks` on WorkService with the parent `storyId`.
3. WHEN the user clicks a task in the task list, THE Frontend_App SHALL open a task detail panel (slide-over or modal) showing: Title, Description, Status, Task Type, Priority, Assignee, Department (auto-mapped from task type), Estimated Hours, Actual Hours, Due Date, Comments, and Activity Log.
4. THE Frontend_App SHALL display workflow transition buttons for tasks based on valid transitions: ToDo→InProgress (requires assignee), InProgress→InReview, InReview→InProgress, InReview→Done.
5. WHEN a task transition button is clicked, THE Frontend_App SHALL call `PATCH /api/v1/tasks/{id}/status` on WorkService.
6. THE Frontend_App SHALL display an "Assign" button (visible to DeptLead+) and a "Self-Assign" button (visible to Member+). WHEN "Assign" is clicked, THE Frontend_App SHALL open a member selector filtered to the task's department. WHEN "Self-Assign" is clicked, THE Frontend_App SHALL call `PATCH /api/v1/tasks/{id}/self-assign`.
7. WHEN `ASSIGNEE_NOT_IN_DEPARTMENT` (4018) is returned, THE Frontend_App SHALL display "Selected member is not in the required department."
8. WHEN `ASSIGNEE_AT_CAPACITY` (4019) is returned, THE Frontend_App SHALL display "Selected member has reached their maximum concurrent tasks."
9. THE Frontend_App SHALL display a "Suggest Assignee" button that calls `GET /api/v1/tasks/suggest-assignee` on WorkService and pre-fills the assignee selector with the suggestion.
10. THE Frontend_App SHALL display a "Log Hours" button that opens a form with hours (number, positive) and description (text). WHEN submitted, THE Frontend_App SHALL call `PATCH /api/v1/tasks/{id}/log-hours` on WorkService.
11. THE Frontend_App SHALL validate the task form client-side: Title required, Task Type required and must be one of the 6 valid types, Estimated Hours must be positive if provided.

### Requirement 18: Sprint Management

**User Story:** As a DeptLead or OrgAdmin, I want to create and manage sprints so that work is organized into time-boxed iterations.

#### Acceptance Criteria

1. WHEN the user navigates to `/sprints`, THE Frontend_App SHALL call `GET /api/v1/sprints` on WorkService and display a list of sprints with: Sprint Name, Project, Status badge, Start Date, End Date, Story Count, Velocity (if completed), and Progress bar.
2. WHEN the user clicks "Create Sprint" (visible to OrgAdmin, DeptLead), THE Frontend_App SHALL open a form with fields: Project (dropdown, required), Sprint Name (required), Goal (text), Start Date (date picker), End Date (date picker).
3. WHEN the create sprint form is submitted, THE Frontend_App SHALL call `POST /api/v1/projects/{projectId}/sprints` on WorkService.
4. WHEN `SPRINT_END_BEFORE_START` (4033) is returned, THE Frontend_App SHALL display "End date must be after start date."
5. THE Frontend_App SHALL provide sprint lifecycle action buttons based on current status: "Start Sprint" (Planning→Active), "Complete Sprint" (Active→Completed), "Cancel Sprint" (Active→Cancelled).
6. WHEN "Start Sprint" is clicked, THE Frontend_App SHALL call `PATCH /api/v1/sprints/{id}/start`. WHEN `ONLY_ONE_ACTIVE_SPRINT` (4016) is returned, THE Frontend_App SHALL display "Another sprint is already active for this project."
7. WHEN "Complete Sprint" is clicked, THE Frontend_App SHALL call `PATCH /api/v1/sprints/{id}/complete` and display a confirmation dialog showing incomplete stories that will be moved back to backlog.
8. THE Frontend_App SHALL filter sprints by project using a project dropdown filter.
9. THE Frontend_App SHALL validate the sprint form client-side: Sprint Name required, Start Date required, End Date required and must be after Start Date.

### Requirement 19: Sprint Planning

**User Story:** As a DeptLead, I want to drag stories from the backlog into a sprint so that the sprint backlog is properly planned.

#### Acceptance Criteria

1. WHEN the user navigates to `/sprints/:id` for a sprint in `Planning` status, THE Frontend_App SHALL render a split view: backlog stories (left panel) and sprint stories (right panel).
2. THE Frontend_App SHALL use dnd-kit to enable drag-and-drop of stories from the backlog panel to the sprint panel.
3. WHEN a story is dropped into the sprint panel, THE Frontend_App SHALL call `POST /api/v1/sprints/{sprintId}/stories` on WorkService with the story ID.
4. WHEN a story is dragged out of the sprint panel back to the backlog, THE Frontend_App SHALL call `DELETE /api/v1/sprints/{sprintId}/stories/{storyId}` on WorkService.
5. WHEN `STORY_ALREADY_IN_SPRINT` (4007) is returned, THE Frontend_App SHALL display "This story is already in the sprint."
6. WHEN `SPRINT_NOT_IN_PLANNING` (4006) is returned, THE Frontend_App SHALL display "Stories can only be added during sprint planning."
7. WHEN `STORY_PROJECT_MISMATCH` (4046) is returned, THE Frontend_App SHALL display "This story belongs to a different project."
8. THE Frontend_App SHALL display the sprint's total story points and story count in the sprint panel header, updating in real-time as stories are added or removed.

### Requirement 20: Sprint Detail and Metrics

**User Story:** As a project manager, I want sprint metrics and burndown data so that I can track iteration progress.

#### Acceptance Criteria

1. WHEN the user navigates to `/sprints/:id`, THE Frontend_App SHALL call `GET /api/v1/sprints/{id}` and `GET /api/v1/sprints/{id}/metrics` on WorkService.
2. THE Frontend_App SHALL display sprint metrics: Total Stories, Completed Stories, Total Story Points, Completed Story Points, Completion Rate (%), Velocity, Stories by Status (bar chart), and Tasks by Department (bar chart).
3. THE Frontend_App SHALL render a burndown chart using Recharts showing: Ideal Remaining Points (linear decrease line) and Actual Remaining Points (actual line) over the sprint duration.
4. THE Frontend_App SHALL display the sprint's stories in a table with status, priority, assignee, and story points.
5. WHEN the sprint is Active, THE Frontend_App SHALL auto-refresh metrics every 5 minutes.

### Requirement 21: Kanban Board

**User Story:** As a team member, I want a Kanban board with drag-and-drop so that I can visualize and manage work in progress.

#### Acceptance Criteria

1. WHEN the user navigates to `/boards/kanban`, THE Frontend_App SHALL call `GET /api/v1/boards/kanban` on WorkService and render columns for each workflow status: Backlog, Ready, InProgress, InReview, QA, Done, Closed.
2. THE Frontend_App SHALL render each story as a card showing: Story Key, Title, Priority badge, Story Points, Assignee avatar, Labels (colored dots), Task progress (e.g., "3/5 tasks"), and Project name.
3. THE Frontend_App SHALL use dnd-kit to enable drag-and-drop of story cards between columns. WHEN a card is dropped in a new column, THE Frontend_App SHALL call `PATCH /api/v1/stories/{id}/status` on WorkService with the target status.
4. WHEN a drag-and-drop transition fails (e.g., `INVALID_STORY_TRANSITION` 4004 or missing prerequisites), THE Frontend_App SHALL revert the card to its original column and display a toast with the error message.
5. THE Frontend_App SHALL display column headers with: Status name, Card count, and Total story points.
6. THE Frontend_App SHALL provide filter controls: Project (dropdown), Department (dropdown), Assignee (searchable dropdown), Priority (multi-select), Labels (multi-select).
7. WHEN a story card is clicked, THE Frontend_App SHALL navigate to `/stories/:id`.
8. THE Frontend_App SHALL support a project filter dropdown at the top of the board to scope the view to a single project.

### Requirement 22: Sprint Board

**User Story:** As a team member, I want a sprint board showing tasks grouped by status so that I can track daily progress.

#### Acceptance Criteria

1. WHEN the user navigates to `/boards/sprint`, THE Frontend_App SHALL call `GET /api/v1/boards/sprint` on WorkService and render columns for task statuses: ToDo, InProgress, InReview, Done.
2. THE Frontend_App SHALL render each task as a card showing: Task Title, Parent Story Key, Task Type badge, Assignee avatar, Department badge, and Priority.
3. THE Frontend_App SHALL use dnd-kit to enable drag-and-drop of task cards between columns. WHEN a card is dropped, THE Frontend_App SHALL call `PATCH /api/v1/tasks/{id}/status` on WorkService.
4. WHEN a drag-and-drop transition fails, THE Frontend_App SHALL revert the card and display a toast.
5. THE Frontend_App SHALL provide a project filter and sprint selector at the top of the board.
6. THE Frontend_App SHALL display the active sprint's name, remaining days, and progress bar in the board header.

### Requirement 23: Department Board

**User Story:** As a DeptLead, I want a department board showing tasks grouped by department so that I can see workload distribution.

#### Acceptance Criteria

1. WHEN the user navigates to `/boards/department`, THE Frontend_App SHALL call `GET /api/v1/boards/department` on WorkService and render columns for each department (Engineering, QA, DevOps, Product, Design, plus custom departments).
2. THE Frontend_App SHALL render each task as a card showing: Task Title, Parent Story Key, Status badge, Assignee avatar, and Priority.
3. THE Frontend_App SHALL display column headers with: Department name, Task count, and workload indicator (tasks vs. capacity).
4. THE Frontend_App SHALL provide a project filter at the top of the board.

### Requirement 24: Backlog View

**User Story:** As a team member, I want a backlog view showing prioritized stories not in a sprint so that I can plan upcoming work.

#### Acceptance Criteria

1. WHEN the user navigates to `/boards/backlog`, THE Frontend_App SHALL call `GET /api/v1/boards/backlog` on WorkService and display stories not assigned to any sprint, sorted by priority (Critical first).
2. THE Frontend_App SHALL render each story with: Story Key, Title, Priority badge, Story Points, Assignee, Labels, and Created Date.
3. THE Frontend_App SHALL provide a project filter at the top of the backlog.
4. THE Frontend_App SHALL support drag-and-drop reordering of backlog stories by priority.
5. THE Frontend_App SHALL display a "Add to Sprint" action on each story that opens a sprint selector (showing sprints in Planning status).

### Requirement 25: Comments

**User Story:** As a team member, I want to add threaded comments with @mentions on stories and tasks so that I can collaborate with my team.

#### Acceptance Criteria

1. THE Frontend_App SHALL display a comment section on story detail and task detail pages, showing threaded comments with: Author name, Author avatar, Timestamp, Content (Markdown rendered), and Edit/Delete actions (Edit visible to author only, Delete visible to author and OrgAdmin).
2. THE Frontend_App SHALL provide a comment input with Markdown support (bold, italic, code, links, lists) and a preview toggle.
3. WHEN the user types `@` in the comment input, THE Frontend_App SHALL display an autocomplete dropdown of team members (from `GET /api/v1/team-members` on ProfileService) filtered by the typed text.
4. WHEN a comment is submitted, THE Frontend_App SHALL call `POST /api/v1/comments` on WorkService with `entityType` (Story or Task), `entityId`, `content`, and optional `parentCommentId` for replies.
5. WHEN the user clicks "Reply" on a comment, THE Frontend_App SHALL open a nested comment input with the parent comment ID set.
6. WHEN the user clicks "Edit" on their own comment, THE Frontend_App SHALL replace the comment content with an editable input pre-filled with the existing content. WHEN submitted, THE Frontend_App SHALL call `PUT /api/v1/comments/{id}` on WorkService.
7. WHEN the user clicks "Delete" on a comment, THE Frontend_App SHALL show a confirmation dialog. WHEN confirmed, THE Frontend_App SHALL call `DELETE /api/v1/comments/{id}` on WorkService.
8. WHEN `COMMENT_NOT_AUTHOR` (4017) is returned, THE Frontend_App SHALL display "Only the comment author can edit or delete this comment."
9. WHEN `MENTION_USER_NOT_FOUND` (4029) is returned, THE Frontend_App SHALL display "One or more mentioned users were not found."

### Requirement 26: Team Member Management

**User Story:** As an OrgAdmin or DeptLead, I want to view and manage team members so that the organization is properly staffed.

#### Acceptance Criteria

1. WHEN the user navigates to `/members`, THE Frontend_App SHALL call `GET /api/v1/team-members` on ProfileService and display a paginated list with: Name, Professional ID (e.g., `NXS-ENG-001`), Email, Department(s), Role(s), Availability badge, and Status badge.
2. THE Frontend_App SHALL provide filter controls for: Department (dropdown), Role (dropdown), Status (dropdown), and Availability (dropdown).
3. WHEN the user clicks a member row, THE Frontend_App SHALL navigate to `/members/:id` and display the full profile including: all department memberships with roles, skills, availability, `MaxConcurrentTasks`, current active task count, and recent activity.
4. WHEN an OrgAdmin views a member profile, THE Frontend_App SHALL display management actions: Change Role (per department), Add to Department, Remove from Department, Change Status (Activate/Suspend/Deactivate).
5. WHEN "Change Role" is clicked, THE Frontend_App SHALL open a dialog with a role dropdown (OrgAdmin, DeptLead, Member, Viewer) and call `PATCH /api/v1/team-members/{id}/departments/{deptId}/role` on ProfileService.
6. WHEN "Add to Department" is clicked, THE Frontend_App SHALL open a dialog with department and role selectors and call `POST /api/v1/team-members/{id}/departments` on ProfileService.
7. WHEN `MEMBER_ALREADY_IN_DEPARTMENT` (3011) is returned, THE Frontend_App SHALL display "Member is already in this department."
8. WHEN `LAST_ORGADMIN_CANNOT_DEACTIVATE` (3004) is returned, THE Frontend_App SHALL display "Cannot deactivate the last OrgAdmin."
9. THE Frontend_App SHALL display a capacity view showing each member's current task load vs. `MaxConcurrentTasks` as a progress bar.
10. WHEN a member clicks their own profile, THE Frontend_App SHALL allow editing: Availability (Available/Busy/Away/Offline), MaxConcurrentTasks, and profile fields.

### Requirement 27: Department Management

**User Story:** As an OrgAdmin, I want to manage departments so that the organization structure is maintained.

#### Acceptance Criteria

1. WHEN the user navigates to `/departments`, THE Frontend_App SHALL call `GET /api/v1/departments` on ProfileService and display a list of departments with: Name, Code, Member Count, Is Default badge, and Status.
2. WHEN the user clicks "Create Department" (visible to OrgAdmin), THE Frontend_App SHALL open a form with: Department Name (required), Department Code (required, uppercase), and Description.
3. WHEN the create form is submitted, THE Frontend_App SHALL call `POST /api/v1/departments` on ProfileService.
4. WHEN `DEPARTMENT_NAME_DUPLICATE` (3008) is returned, THE Frontend_App SHALL display "A department with this name already exists."
5. WHEN `DEPARTMENT_CODE_DUPLICATE` (3009) is returned, THE Frontend_App SHALL display "A department with this code already exists."
6. WHEN the user clicks a department row, THE Frontend_App SHALL navigate to `/departments/:id` and display: department details, member list with roles, and department preferences.
7. WHEN a default department is targeted for deletion, THE Frontend_App SHALL display "Default departments cannot be deleted" and disable the delete button.

### Requirement 28: Organization Settings

**User Story:** As an OrgAdmin, I want to configure organization-level settings so that the platform adapts to our workflow.

#### Acceptance Criteria

1. WHEN the user navigates to `/settings` (OrgAdmin only), THE Frontend_App SHALL call `GET /api/v1/organizations/{id}` and `GET /api/v1/organizations/{id}/settings` on ProfileService and display settings grouped by category: General, Workflow, Board, Notification, Data.
2. THE Frontend_App SHALL render the following settings fields:

| Category | Field | Input Type |
|----------|-------|------------|
| General | Story ID Prefix | Text (2–10 uppercase alphanumeric) |
| General | Timezone | Timezone selector (IANA) |
| General | Default Sprint Duration | Number (1–4 weeks) |
| General | Working Days | Multi-select checkboxes |
| General | Working Hours Start/End | Time pickers |
| General | Logo URL | URL input |
| General | Primary Color | Color picker |
| Workflow | Story Point Scale | Dropdown (Fibonacci, Linear, Powers of 2) |
| Workflow | Auto-Assignment Enabled | Toggle |
| Workflow | Auto-Assignment Strategy | Dropdown (LeastLoaded, RoundRobin) |
| Board | Default Board View | Dropdown (Kanban, Sprint, Backlog) |
| Board | WIP Limits Enabled | Toggle |
| Board | Default WIP Limit | Number |
| Notification | Default Channels | Multi-select (Email, Push, InApp) |
| Notification | Digest Frequency | Dropdown (Realtime, Hourly, Daily, Off) |
| Data | Audit Retention Days | Number |

3. WHEN the settings form is submitted, THE Frontend_App SHALL call `PUT /api/v1/organizations/{id}/settings` on ProfileService.
4. WHEN `STORY_PREFIX_INVALID_FORMAT` (3020) is returned, THE Frontend_App SHALL display "Story ID prefix must be 2–10 uppercase alphanumeric characters."
5. WHEN `STORY_PREFIX_DUPLICATE` (3006) is returned, THE Frontend_App SHALL display "This story ID prefix is already in use by another organization."
6. WHEN `STORY_PREFIX_IMMUTABLE` (3007) is returned, THE Frontend_App SHALL display "Story ID prefix cannot be changed after stories have been created."
7. THE Frontend_App SHALL validate settings client-side: Story ID Prefix matches `^[A-Z0-9]{2,10}$`, Sprint Duration 1–4, Working Hours Start before End.

### Requirement 29: User Preferences

**User Story:** As a team member, I want to configure my personal preferences so that the platform adapts to my workflow.

#### Acceptance Criteria

1. WHEN the user navigates to `/preferences`, THE Frontend_App SHALL call `GET /api/v1/preferences` on ProfileService and display the user's preferences.
2. THE Frontend_App SHALL render the following preference fields:

| Field | Input Type |
|-------|------------|
| Theme | Dropdown (Light, Dark, System) |
| Language | Dropdown (language codes) |
| Timezone Override | Timezone selector (IANA) |
| Default Board View | Dropdown (Kanban, Sprint, Backlog) |
| Email Digest Frequency | Dropdown (Realtime, Hourly, Daily, Off) |
| Keyboard Shortcuts Enabled | Toggle |
| Date Format | Dropdown (ISO, US, EU) |
| Time Format | Dropdown (24h, 12h) |

3. WHEN any preference is changed, THE Frontend_App SHALL call `PUT /api/v1/preferences` on ProfileService.
4. WHEN the Theme preference is changed, THE Frontend_App SHALL immediately apply the new theme (light/dark/system) without a page reload.
5. WHEN `INVALID_PREFERENCE_VALUE` (3026) is returned, THE Frontend_App SHALL display "Invalid preference value."
6. THE Frontend_App SHALL show a "Resolved Preferences" section that calls `GET /api/v1/preferences/resolved` on ProfileService and displays the effective preferences after cascade resolution (User → Department → Organization → System Default), indicating which level each value comes from.

### Requirement 30: Theme Management (Theme Store)

**User Story:** As a user, I want light, dark, and system theme support so that the interface matches my preference.

#### Acceptance Criteria

1. THE Frontend_App SHALL create a Zustand theme store containing: `theme` (enum: Light, Dark, System) and `resolvedTheme` (enum: Light, Dark — the actual applied theme).
2. WHEN `theme` is set to `System`, THE Frontend_App SHALL detect the OS preference via `window.matchMedia('(prefers-color-scheme: dark)')` and set `resolvedTheme` accordingly.
3. WHEN the OS theme changes while `theme` is `System`, THE Frontend_App SHALL update `resolvedTheme` in real-time.
4. THE Frontend_App SHALL apply the theme by toggling a `dark` class on the `<html>` element (Tailwind CSS dark mode).
5. THE Frontend_App SHALL persist the theme preference in the Zustand store and sync it with the backend user preferences.

### Requirement 31: Invitation Management

**User Story:** As an OrgAdmin or DeptLead, I want to invite new team members so that onboarding is streamlined.

#### Acceptance Criteria

1. WHEN the user navigates to `/invites` (OrgAdmin or DeptLead), THE Frontend_App SHALL call `GET /api/v1/invites` on ProfileService and display a list of pending invites with: Email, Name, Department, Role, Expiry Date, and Status.
2. WHEN the user clicks "Create Invite", THE Frontend_App SHALL open a form with: Email (required), First Name (required), Last Name (required), Department (dropdown — OrgAdmin sees all, DeptLead sees own department only), and Role (dropdown).
3. WHEN the invite form is submitted, THE Frontend_App SHALL call `POST /api/v1/invites` on ProfileService.
4. WHEN `INVITE_EMAIL_ALREADY_MEMBER` (3014) is returned, THE Frontend_App SHALL display "This email is already registered as a member."
5. WHEN the user clicks "Cancel Invite", THE Frontend_App SHALL call `DELETE /api/v1/invites/{id}` on ProfileService after confirmation.
6. THE Frontend_App SHALL validate the invite form client-side: Email must be valid format, First Name and Last Name required.

### Requirement 32: Accept Invite Flow

**User Story:** As an invited user, I want to accept an invitation via a public page so that I can join the organization.

#### Acceptance Criteria

1. WHEN a user navigates to `/invites/:token` (no auth required), THE Frontend_App SHALL call `GET /api/v1/invites/{token}/validate` on ProfileService and display the invite details: Organization Name, Department Name, and Role.
2. WHEN the invite is valid, THE Frontend_App SHALL display an acceptance form with OTP verification (the OTP is sent to the invitee's email).
3. WHEN the user enters the OTP and clicks "Accept", THE Frontend_App SHALL call `POST /api/v1/invites/{token}/accept` on ProfileService.
4. WHEN acceptance succeeds, THE Frontend_App SHALL redirect to `/login` with a success toast "Welcome! Please log in with your credentials."
5. WHEN `INVITE_EXPIRED_OR_INVALID` (3002) is returned, THE Frontend_App SHALL display "This invitation has expired or is no longer valid."

### Requirement 33: Device and Session Management

**User Story:** As a team member, I want to manage my devices and active sessions so that I can control access to my account.

#### Acceptance Criteria

1. WHEN the user navigates to `/sessions`, THE Frontend_App SHALL call `GET /api/v1/devices` on ProfileService and `GET /api/v1/sessions` on SecurityService.
2. THE Frontend_App SHALL display a Devices section showing: Device Name, Device Type (Desktop/Mobile/Tablet) with icon, Is Primary badge, IP Address, Last Active Date, and a "Remove" button.
3. THE Frontend_App SHALL display an Active Sessions section showing: Device info, IP Address, Created timestamp, and a "Revoke" button.
4. WHEN the user clicks "Revoke" on a session, THE Frontend_App SHALL call `DELETE /api/v1/sessions/{sessionId}` on SecurityService after confirmation.
5. THE Frontend_App SHALL provide a "Revoke All Other Sessions" button that calls `DELETE /api/v1/sessions/all` on SecurityService.
6. WHEN the user clicks "Set as Primary" on a device, THE Frontend_App SHALL call `PATCH /api/v1/devices/{id}/primary` on ProfileService.
7. WHEN the user clicks "Remove" on a device, THE Frontend_App SHALL call `DELETE /api/v1/devices/{id}` on ProfileService after confirmation.
8. WHEN `MAX_DEVICES_REACHED` (3003) is returned, THE Frontend_App SHALL display "Maximum of 5 devices reached. Remove a device first."

### Requirement 34: Notification Preferences

**User Story:** As a team member, I want to configure my notification preferences so that I only receive relevant notifications.

#### Acceptance Criteria

1. THE Frontend_App SHALL display notification preferences on the `/preferences` page (or a sub-tab) by calling `GET /api/v1/notification-settings` on ProfileService.
2. THE Frontend_App SHALL render a table with rows for each of the 8 notification types (StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching) and columns for each channel (Email, Push, InApp) as toggle switches.
3. WHEN a toggle is changed, THE Frontend_App SHALL call `PUT /api/v1/notification-settings/{typeId}` on ProfileService with the updated channel preferences.
4. THE Frontend_App SHALL display the notification type names in human-readable format (e.g., "Story Assigned", "Sprint Started").

### Requirement 35: Global Search

**User Story:** As a team member, I want to search across stories and tasks so that I can find work items quickly.

#### Acceptance Criteria

1. WHEN the user types in the global search input (header) and presses Enter or clicks search, THE Frontend_App SHALL navigate to `/search?q={query}` and call `GET /api/v1/search?q={query}` on WorkService.
2. THE Frontend_App SHALL display search results grouped by type (Stories, Tasks) with: Story Key/Task Title, Status badge, Priority badge, Assignee, and a snippet of matching text.
3. THE Frontend_App SHALL provide filter controls on the search results page: Type (Stories/Tasks/All), Project, Status, Priority, and Date Range.
4. WHEN the search query is less than 2 characters, THE Frontend_App SHALL display "Search query must be at least 2 characters" and not make an API call.
5. THE Frontend_App SHALL debounce the search input by 300ms to avoid excessive API calls.
6. WHEN a search result is clicked, THE Frontend_App SHALL navigate to the corresponding detail page (`/stories/:id` or task detail).

### Requirement 36: Saved Filters

**User Story:** As a team member, I want to save and load custom filter configurations so that I can quickly access my preferred views.

#### Acceptance Criteria

1. THE Frontend_App SHALL display a "Save Filter" button on board pages and the search page when filters are active.
2. WHEN "Save Filter" is clicked, THE Frontend_App SHALL open a dialog with a name input. WHEN submitted, THE Frontend_App SHALL call `POST /api/v1/saved-filters` on WorkService with the filter name and current filter JSON.
3. THE Frontend_App SHALL display a "Saved Filters" dropdown on board pages and the search page that lists the user's saved filters (from `GET /api/v1/saved-filters` on WorkService).
4. WHEN a saved filter is selected, THE Frontend_App SHALL apply the filter configuration to the current view.
5. THE Frontend_App SHALL provide a "Delete" action on each saved filter that calls `DELETE /api/v1/saved-filters/{id}` on WorkService after confirmation.

### Requirement 37: Reports

**User Story:** As a project manager, I want reports and charts so that I can track team performance and project health.

#### Acceptance Criteria

1. WHEN the user navigates to `/reports`, THE Frontend_App SHALL display a reports dashboard with tabs or cards for each report type: Velocity, Department Workload, Capacity Utilization, Cycle Time, Task Completion Rate.
2. WHEN the Velocity report is selected, THE Frontend_App SHALL call `GET /api/v1/reports/velocity` on WorkService and render a bar chart (Recharts) showing velocity per sprint with an average trend line.
3. WHEN the Department Workload report is selected, THE Frontend_App SHALL call `GET /api/v1/reports/department-workload` on WorkService and render a stacked bar chart showing task distribution across departments.
4. WHEN the Capacity Utilization report is selected, THE Frontend_App SHALL call `GET /api/v1/reports/capacity` on WorkService and render a chart showing team member utilization (active tasks / max concurrent tasks) per department.
5. WHEN the Cycle Time report is selected, THE Frontend_App SHALL call `GET /api/v1/reports/cycle-time` on WorkService and render a line chart showing average time from story creation to completion over time.
6. WHEN the Task Completion Rate report is selected, THE Frontend_App SHALL call `GET /api/v1/reports/task-completion` on WorkService and render a chart showing task completion rates by department and task type.
7. THE Frontend_App SHALL provide date range and project filters on all reports.

### Requirement 38: PlatformAdmin — Organization Management

**User Story:** As a PlatformAdmin, I want to manage organizations so that I can onboard new tenants.

#### Acceptance Criteria

1. WHEN a PlatformAdmin navigates to `/admin/organizations`, THE Frontend_App SHALL call `GET /api/v1/organizations` on ProfileService (cross-org, no organization scope) and display a list of all organizations with: Name, Status, Member Count, Created Date.
2. WHEN the PlatformAdmin clicks "Create Organization", THE Frontend_App SHALL open a form with: Organization Name (required), Description, Website, and Story ID Prefix (required).
3. WHEN the create form is submitted, THE Frontend_App SHALL call `POST /api/v1/organizations` on ProfileService.
4. WHEN the PlatformAdmin clicks "Provision Admin" on an organization, THE Frontend_App SHALL open a form with: Email, First Name, Last Name. WHEN submitted, THE Frontend_App SHALL call `POST /api/v1/organizations/{id}/provision-admin` on ProfileService.
5. WHEN `ORGANIZATION_NAME_DUPLICATE` (3005) is returned, THE Frontend_App SHALL display "An organization with this name already exists."
6. THE Frontend_App SHALL display organization status management actions: Activate, Suspend, Deactivate.

### Requirement 39: Error Handling and Toast Notifications

**User Story:** As a user, I want clear error messages and feedback so that I understand what happened when something goes wrong.

#### Acceptance Criteria

1. WHEN any API call fails, THE Frontend_App SHALL parse the `ApiResponse` error envelope and display a toast notification with the error message.
2. THE Frontend_App SHALL map known backend error codes to user-friendly messages (e.g., `VALIDATION_ERROR` → "Please fix the highlighted fields", `STORY_NOT_FOUND` → "Story not found").
3. WHEN a validation error (HTTP 422) is returned with per-field errors, THE Frontend_App SHALL highlight the specific form fields with inline error messages.
4. WHEN an unexpected error (HTTP 500) occurs, THE Frontend_App SHALL display "Something went wrong. Please try again." without exposing technical details.
5. WHEN a network error occurs (no response), THE Frontend_App SHALL display "Unable to connect to the server. Check your connection."
6. THE Frontend_App SHALL use a toast component (from Shadcn/ui) positioned at the top-right with auto-dismiss after 5 seconds for success toasts and manual dismiss for error toasts.
7. WHEN an API call succeeds for a create/update/delete operation, THE Frontend_App SHALL display a success toast (e.g., "Story created successfully", "Sprint started").

### Requirement 40: Loading States

**User Story:** As a user, I want visual loading indicators so that I know when data is being fetched.

#### Acceptance Criteria

1. WHEN any API call is in progress, THE Frontend_App SHALL display a loading indicator appropriate to the context: skeleton loaders for page content, spinner overlays for form submissions, and inline spinners for button actions.
2. THE Frontend_App SHALL disable form submit buttons while a submission is in progress to prevent duplicate requests.
3. WHEN a page loads, THE Frontend_App SHALL render skeleton placeholders matching the expected layout until data arrives.
4. WHEN a board view loads, THE Frontend_App SHALL display skeleton column placeholders.
5. WHEN a drag-and-drop operation is in progress, THE Frontend_App SHALL show an optimistic update (move the card immediately) and revert if the API call fails.

### Requirement 41: Form Validation

**User Story:** As a developer, I want consistent client-side form validation so that invalid data is caught before API calls.

#### Acceptance Criteria

1. THE Frontend_App SHALL use React Hook Form for all form state management and Zod schemas for validation, mirroring the backend FluentValidation rules.
2. THE Frontend_App SHALL define Zod schemas for all form inputs including: Login (email format, password not empty), Story Create/Edit (title required max 200, description max 5000, story points Fibonacci, priority enum), Task Create/Edit (title required, task type enum), Sprint Create (name required, dates required, end after start), Invite (email format, names required), Organization Settings (prefix format, sprint duration 1–4), Password (complexity rules).
3. WHEN a form field fails validation, THE Frontend_App SHALL display an inline error message below the field immediately on blur or on submit.
4. THE Frontend_App SHALL prevent form submission when validation errors exist.
5. THE Frontend_App SHALL clear field-level errors when the user corrects the input.

### Requirement 42: Responsive Design

**User Story:** As a user, I want the application to work on desktop and tablet so that I can use it on different devices.

#### Acceptance Criteria

1. THE Frontend_App SHALL be designed desktop-first with responsive breakpoints for tablet (768px–1024px).
2. WHEN the viewport is tablet-sized, THE Frontend_App SHALL collapse the sidebar to icon-only mode by default.
3. WHEN the viewport is tablet-sized, THE Frontend_App SHALL stack board columns vertically or enable horizontal scrolling.
4. THE Frontend_App SHALL use Tailwind CSS responsive utilities for layout adjustments.
5. THE Frontend_App SHALL ensure all interactive elements have minimum touch target sizes of 44x44px on tablet viewports.

### Requirement 43: Accessibility

**User Story:** As a user, I want the application to be accessible so that it can be used with assistive technologies.

#### Acceptance Criteria

1. THE Frontend_App SHALL use semantic HTML elements: `<nav>` for navigation, `<main>` for content, `<header>` for the top bar, `<aside>` for the sidebar, `<button>` for actions, `<table>` for tabular data.
2. THE Frontend_App SHALL provide ARIA labels on all interactive elements that lack visible text labels (icon buttons, avatar buttons, toggle switches).
3. THE Frontend_App SHALL support keyboard navigation: Tab for focus traversal, Enter/Space for activation, Escape for closing modals/dropdowns, Arrow keys for menu navigation.
4. THE Frontend_App SHALL manage focus correctly: move focus into modals when opened, return focus to the trigger when closed, trap focus within modals.
5. THE Frontend_App SHALL announce dynamic content changes (toast notifications, loading states, form errors) via ARIA live regions.
6. THE Frontend_App SHALL ensure sufficient color contrast ratios (minimum 4.5:1 for normal text, 3:1 for large text) in both light and dark themes.
7. THE Frontend_App SHALL provide visible focus indicators on all focusable elements.

### Requirement 44: Organization State Management (Org Store)

**User Story:** As a developer, I want a centralized organization store so that organization context is accessible throughout the application.

#### Acceptance Criteria

1. THE Frontend_App SHALL create a Zustand organization store containing: `organization` (object with `organizationId`, `organizationName`, `storyIdPrefix`, `timeZone`, `settings`), `departments` (array), and `referenceData` (department types, priority levels, task types, workflow states).
2. WHEN the user logs in, THE Frontend_App SHALL fetch the organization details from `GET /api/v1/organizations/{id}` on ProfileService and populate the org store.
3. THE Frontend_App SHALL fetch reference data from UtilityService (`GET /api/v1/reference/department-types`, `GET /api/v1/reference/priority-levels`, `GET /api/v1/reference/task-types`, `GET /api/v1/reference/workflow-states`) on application load and cache it in the org store.
4. WHEN organization settings are updated, THE Frontend_App SHALL refresh the org store.

### Requirement 45: Department Preferences

**User Story:** As a DeptLead, I want to configure department-specific preferences so that my team's workflow is optimized.

#### Acceptance Criteria

1. WHEN the user navigates to `/departments/:id` and has DeptLead or OrgAdmin role, THE Frontend_App SHALL display a "Preferences" tab showing department preferences from `GET /api/v1/departments/{id}/preferences` on ProfileService.
2. THE Frontend_App SHALL render preference fields: Default Task Types (multi-select), WIP Limit Per Status (key-value editor), Default Assignee (member selector), Notification Channel Overrides (channel toggles), and Max Concurrent Tasks Default (number).
3. WHEN preferences are updated, THE Frontend_App SHALL call `PUT /api/v1/departments/{id}/preferences` on ProfileService.

### Requirement 46: Velocity History

**User Story:** As a project manager, I want to view velocity history across sprints so that I can forecast future capacity.

#### Acceptance Criteria

1. WHEN the user views the Velocity report or the dashboard velocity widget, THE Frontend_App SHALL call `GET /api/v1/sprints/velocity?count=10` on WorkService.
2. THE Frontend_App SHALL render a bar chart using Recharts with sprint names on the x-axis and velocity (story points) on the y-axis.
3. THE Frontend_App SHALL display an average velocity line across the chart.
4. THE Frontend_App SHALL allow the user to adjust the number of sprints shown (5, 10, 15, 20).

### Requirement 47: Activity Log

**User Story:** As a team member, I want to see the activity history for stories and tasks so that I can track all changes.

#### Acceptance Criteria

1. WHEN the user views a story detail page, THE Frontend_App SHALL call `GET /api/v1/stories/{id}/activity` on WorkService and display a chronological timeline.
2. WHEN the user views a task detail page, THE Frontend_App SHALL call `GET /api/v1/tasks/{id}/activity` on WorkService and display a chronological timeline.
3. THE Frontend_App SHALL render each activity entry with: Actor name, Action description (e.g., "changed status from InProgress to InReview"), Old Value → New Value (for field changes), and Timestamp.
4. THE Frontend_App SHALL use icons to differentiate activity types: status change (arrow), assignment (person), comment (chat bubble), label change (tag), edit (pencil).

### Requirement 48: Notification History

**User Story:** As a team member, I want to view my notification history so that I can review past notifications.

#### Acceptance Criteria

1. WHEN the user clicks the notification bell icon in the header, THE Frontend_App SHALL display a dropdown panel showing recent notifications from `GET /api/v1/notification-logs` on UtilityService.
2. THE Frontend_App SHALL display each notification with: Type icon, Subject, Timestamp, and Status (Sent/Failed).
3. THE Frontend_App SHALL provide a "View All" link that navigates to a full notification history page with pagination and filters (type, channel, status, date range).
4. THE Frontend_App SHALL display an unread count badge on the notification bell icon.

### Requirement 49: TypeScript Type Definitions

**User Story:** As a developer, I want comprehensive TypeScript type definitions so that the frontend codebase is type-safe and matches backend DTOs.

#### Acceptance Criteria

1. THE Frontend_App SHALL define TypeScript interfaces for all backend DTOs including: `ApiResponse<T>`, `PaginatedResponse<T>`, `ErrorDetail`, `LoginResponse`, `TokenRefreshResponse`, `Story`, `StoryDetail`, `Task`, `TaskDetail`, `Sprint`, `SprintMetrics`, `BurndownDataPoint`, `KanbanBoard`, `KanbanColumn`, `KanbanCard`, `Comment`, `ActivityLogEntry`, `Label`, `StoryLink`, `SavedFilter`.
2. THE Frontend_App SHALL define TypeScript interfaces for ProfileService DTOs: `Organization`, `OrganizationSettings`, `Department`, `DepartmentPreferences`, `TeamMember`, `TeamMemberDetail`, `DepartmentMember`, `Role`, `Invite`, `Device`, `NotificationSetting`, `UserPreferences`, `ResolvedPreferences`.
3. THE Frontend_App SHALL define TypeScript interfaces for UtilityService DTOs: `AuditLog`, `NotificationLog`, `DepartmentType`, `PriorityLevel`, `TaskTypeRef`, `WorkflowState`.
4. THE Frontend_App SHALL define TypeScript enums for: `StoryStatus`, `TaskStatus`, `SprintStatus`, `Priority`, `TaskType`, `Role`, `Availability`, `Theme`, `DateFormat`, `TimeFormat`, `DigestFrequency`, `BoardView`, `LinkType`, `NotificationType`, `FlgStatus`.
5. THE Frontend_App SHALL define TypeScript interfaces for all API request payloads: `LoginRequest`, `CreateStoryRequest`, `UpdateStoryRequest`, `CreateTaskRequest`, `CreateSprintRequest`, `CreateProjectRequest`, `CreateInviteRequest`, `CreateCommentRequest`, `CreateLabelRequest`, `UpdateOrganizationSettingsRequest`, `UpdatePreferencesRequest`.

### Requirement 50: Environment Configuration

**User Story:** As a developer, I want environment-based configuration so that the frontend can target different backend environments.

#### Acceptance Criteria

1. THE Frontend_App SHALL use Vite environment variables prefixed with `VITE_` for all configuration.
2. THE Frontend_App SHALL define the following environment variables in `.env.example`:

| Variable | Description | Default |
|----------|-------------|---------|
| `VITE_SECURITY_API_URL` | SecurityService base URL | `http://localhost:5001` |
| `VITE_PROFILE_API_URL` | ProfileService base URL | `http://localhost:5002` |
| `VITE_WORK_API_URL` | WorkService base URL | `http://localhost:5003` |
| `VITE_UTILITY_API_URL` | UtilityService base URL | `http://localhost:5200` |
| `VITE_APP_NAME` | Application display name | `Nexus 2.0` |

3. THE Frontend_App SHALL validate that all required environment variables are present at build time and throw a clear error if any are missing.

### Requirement 51: Project Structure

**User Story:** As a developer, I want a well-organized project structure so that the codebase is maintainable and scalable.

#### Acceptance Criteria

1. THE Frontend_App SHALL follow the project structure defined in Appendix D:

| Directory | Purpose |
|-----------|---------|
| `src/api/` | Typed API clients per backend service (`securityApi.ts`, `profileApi.ts`, `workApi.ts`, `utilityApi.ts`) |
| `src/components/layout/` | Application shell components (AppShell, Sidebar, Header) |
| `src/components/common/` | Shared UI components (Button, Modal, Table, Pagination, Badge, Toast) |
| `src/components/forms/` | Form components (FormField, PasswordInput, OtpInput) |
| `src/features/` | Feature-based modules (auth, dashboard, stories, tasks, sprints, boards, members, departments, settings, invites, search) |
| `src/hooks/` | Custom React hooks (useAuth, useOrg, useDebounce, usePagination) |
| `src/stores/` | Zustand stores (authStore, orgStore, themeStore) |
| `src/types/` | Shared TypeScript types and interfaces |
| `src/utils/` | Helper functions (date formatting, story key parsing, error mapping) |

2. THE Frontend_App SHALL use feature-based module organization where each feature directory contains its own pages, components, hooks, and API calls.
3. THE Frontend_App SHALL configure path aliases in `tsconfig.json` (e.g., `@/` → `src/`) for clean imports.

### Requirement 52: Testing Setup

**User Story:** As a developer, I want a testing setup so that frontend components and logic can be verified.

#### Acceptance Criteria

1. THE Frontend_App SHALL configure Vitest as the test runner with React Testing Library for component testing.
2. THE Frontend_App SHALL include test utilities for: mocking API clients, rendering components with providers (Router, Zustand stores), and creating test fixtures for common data types.
3. THE Frontend_App SHALL configure test coverage reporting.
4. THE Frontend_App SHALL include example tests for: API client interceptor behavior, auth store actions, route guard logic, and form validation schemas.
