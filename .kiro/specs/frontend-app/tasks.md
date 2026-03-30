# Implementation Plan: Frontend Application

## Overview

Incremental implementation of the Nexus 2.0 Frontend Application — a React 18+ / TypeScript / Vite SPA consuming four backend microservices (SecurityService, ProfileService, WorkService, UtilityService). Tasks are ordered so each step builds on the previous, with no orphaned code. All code lives under `src/frontend/`.

## Tasks

- [x] 1. Project Scaffolding and Configuration
  - [x] 1.1 Initialize Vite + React + TypeScript project under `src/frontend/`
    - Run `npm create vite@latest` with React + TypeScript template
    - Create `index.html`, `src/main.tsx`, `src/App.tsx`
    - Install core dependencies: `react`, `react-dom`, `react-router-dom`, `axios`, `zustand`, `zod`, `react-hook-form`, `@hookform/resolvers`
    - Install UI dependencies: `tailwindcss`, `@tailwindcss/vite`, `shadcn/ui` (via `npx shadcn@latest init`), `lucide-react`
    - Install feature dependencies: `@dnd-kit/core`, `@dnd-kit/sortable`, `recharts`, `date-fns`
    - _Requirements: 51.1_

  - [x] 1.2 Configure Tailwind CSS and Shadcn/ui
    - Set up `tailwind.config.ts` with dark mode (`class` strategy), content paths, and theme extensions
    - Initialize Shadcn/ui with `npx shadcn@latest init` and add base components (Button, Input, Dialog, DropdownMenu, Toast, Card, Table, Tabs, Select, Checkbox, Badge, Tooltip, Sheet, Separator, Avatar, Skeleton)
    - Configure CSS variables for light/dark themes in `src/index.css`
    - _Requirements: 42.4, 43.6_

  - [x] 1.3 Configure path aliases and TypeScript
    - Set up `tsconfig.json` with `@/` path alias pointing to `src/`
    - Update `vite.config.ts` with matching resolve alias
    - Create `src/env.d.ts` with Vite env type declarations for `VITE_SECURITY_API_URL`, `VITE_PROFILE_API_URL`, `VITE_WORK_API_URL`, `VITE_UTILITY_API_URL`, `VITE_APP_NAME`
    - _Requirements: 51.3, 50.1_

  - [x] 1.4 Create environment configuration
    - Create `.env.example` with all required environment variables and defaults
    - Create `src/utils/env.ts` with runtime validation that throws descriptive errors for missing required variables
    - _Requirements: 50.2, 50.3_

  - [x] 1.5 Configure Vitest for testing
    - Create `vitest.config.ts` with jsdom environment, path aliases, coverage config
    - Install dev dependencies: `vitest`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `jsdom`, `fast-check`, `msw`
    - Create `src/test-utils/renderWithProviders.tsx` wrapper with Router + Zustand store providers
    - _Requirements: 52.1, 52.2, 52.3_

- [x] 2. Foundation Layer — Types, Enums, API Client, Stores, Utilities
  - [x] 2.1 Create TypeScript enums (`src/types/enums.ts`)
    - Define all enums: `StoryStatus`, `TaskStatus`, `SprintStatus`, `Priority`, `TaskType`, `LinkType`, `Role`, `Availability`, `Theme`, `DateFormat`, `TimeFormat`, `DigestFrequency`, `BoardView`, `NotificationType`, `FlgStatus`
    - _Requirements: 49.4_

  - [x] 2.2 Create TypeScript type definitions
    - Create `src/types/api.ts` — `ApiResponse<T>`, `PaginatedResponse<T>`, `ErrorDetail`, `ApiError` class
    - Create `src/types/auth.ts` — `LoginRequest`, `LoginResponse`, `RefreshTokenRequest`, `ForcedPasswordChangeRequest`, `PasswordResetRequest`, `PasswordResetConfirmRequest`, `SessionResponse`, `AuthUser`
    - Create `src/types/work.ts` — All story, task, sprint, board, comment, label, search, saved filter, project, and report interfaces
    - Create `src/types/profile.ts` — Organization, department, team member, invite, device, preferences, notification setting interfaces
    - Create `src/types/utility.ts` — AuditLog, NotificationLog, ReferenceData, DepartmentType, PriorityLevel, TaskTypeRef, WorkflowState interfaces
    - _Requirements: 49.1, 49.2, 49.3, 49.5_

  - [x] 2.3 Create base API client with interceptors (`src/api/client.ts`)
    - Implement `createApiClient(options)` factory returning an Axios instance
    - Request interceptor: attach `Authorization: Bearer {accessToken}` from authStore, add `X-Correlation-Id` (UUID)
    - Response interceptor: unwrap `ApiResponse<T>.data` on success, throw `ApiError` on failure
    - 401 handler: attempt token refresh via `POST /api/v1/auth/refresh`, retry original request, redirect to `/login` on failure
    - `REFRESH_TOKEN_REUSE` detection: clear auth state, redirect to `/login`, show session-expired toast
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_

  - [x] 2.4 Create typed API service modules
    - Create `src/api/securityApi.ts` — login, refreshToken, logout, forcedPasswordChange, requestPasswordReset, confirmPasswordReset, getSessions, revokeSession, revokeAllSessions
    - Create `src/api/profileApi.ts` — organization, team members, departments, invites, devices, preferences, notification settings, platform admin endpoints
    - Create `src/api/workApi.ts` — projects, stories, tasks, sprints, boards, comments, search, saved filters, reports, labels endpoints
    - Create `src/api/utilityApi.ts` — audit logs, notification logs, reference data endpoints
    - _Requirements: 1.1, 1.7_

  - [x] 2.5 Create Zustand auth store (`src/stores/authStore.ts`)
    - State: `accessToken`, `refreshToken`, `user` (AuthUser), `isAuthenticated`, `isPlatformAdmin`, `isFirstTimeUser`
    - Actions: `login(tokens, user)`, `logout()`, `refreshTokens(accessToken, refreshToken)`, `setUser(user)`
    - JWT decode on `login` to extract user claims and populate `user` object
    - Persist refresh token securely (encrypted localStorage fallback)
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [x] 2.6 Create Zustand org store (`src/stores/orgStore.ts`)
    - State: `organization`, `departments`, `referenceData`
    - Actions: `setOrganization`, `setDepartments`, `setReferenceData`, `refresh()`
    - Fetch org details and reference data on login
    - _Requirements: 44.1, 44.2, 44.3, 44.4_

  - [x] 2.7 Create Zustand theme store (`src/stores/themeStore.ts`)
    - State: `theme` (Light/Dark/System), `resolvedTheme` (Light/Dark)
    - Listen to `window.matchMedia('(prefers-color-scheme: dark)')` when theme=System
    - Toggle `dark` class on `<html>` element
    - _Requirements: 30.1, 30.2, 30.3, 30.4, 30.5_

  - [x] 2.8 Create utility functions
    - Create `src/utils/errorMapping.ts` — `mapErrorCode(code)` and `mapApiError(error)` with full error code map
    - Create `src/utils/workflowStateMachine.ts` — `storyTransitions`, `taskTransitions`, `getValidTransitions()`, `isValidTransition()`
    - Create `src/utils/dateFormatting.ts` — date/time formatting helpers using `date-fns`
    - Create `src/utils/storyKeyParser.ts` — parse story keys like `NEXUS-42`
    - _Requirements: 39.2, 13.11, 17.4_

  - [x] 2.9 Create shared React hooks
    - Create `src/hooks/useAuth.ts` — convenience hook wrapping authStore selectors
    - Create `src/hooks/useOrg.ts` — convenience hook wrapping orgStore selectors
    - Create `src/hooks/useDebounce.ts` — debounce hook for search input (300ms)
    - Create `src/hooks/usePagination.ts` — pagination state management hook
    - Create `src/hooks/useMediaQuery.ts` — responsive breakpoint detection hook
    - _Requirements: 35.5, 12.5, 42.2_

- [x] 3. Checkpoint — Foundation verification
  - Ensure all types compile, stores initialize, API client builds, and utility functions are wired. Ask the user if questions arise.

- [x] 4. Routing and Route Guards
  - [x] 4.1 Create route guard components
    - Create `AuthGuard` — redirects to `/login` if `!isAuthenticated`, stores intended destination, redirects to `/password/change` if `isFirstTimeUser`
    - Create `RoleGuard` — redirects to `/` with permission-denied toast if `user.roleName` not in allowed roles
    - Create `GuestGuard` — redirects to `/` if `isAuthenticated` (for login/reset pages)
    - Create `FirstTimeGuard` — only allows access when `isFirstTimeUser=true`, redirects to `/` otherwise
    - _Requirements: 8.2, 8.3, 8.4, 4.7_

  - [x] 4.2 Create router configuration (`src/router.tsx`)
    - Define all routes using React Router v6 `createBrowserRouter`
    - Wire guards: GuestGuard on `/login`, `/admin/login`, `/password/reset`; AuthGuard + FirstTimeGuard on `/password/change`; AuthGuard on all app routes; RoleGuard(OrgAdmin) on `/settings`, `/invites`; RoleGuard(PlatformAdmin) on `/admin/organizations`
    - Create `NotFoundPage` for `*` catch-all route
    - Wire `/invites/:token` as public (no auth)
    - _Requirements: 8.1, 8.5, 8.6_

- [x] 5. Layout Components
  - [x] 5.1 Create AppShell layout (`src/components/layout/AppShell.tsx`)
    - Render `<aside>` Sidebar, `<header>` Header, `<main>` content area with `<Outlet />`
    - Support collapsible sidebar with persisted state
    - Use semantic HTML: `<nav>`, `<main>`, `<header>`, `<aside>`
    - _Requirements: 9.1, 43.1_

  - [x] 5.2 Create Sidebar component (`src/components/layout/Sidebar.tsx`)
    - Nav items: Dashboard, Projects, Stories, Boards (expandable: Kanban, Sprint Board, Department Board, Backlog), Sprints, Members, Departments, Reports, Search
    - Conditional items: Settings, Invites (OrgAdmin/DeptLead only)
    - Active item highlighting based on current route via `useLocation()`
    - Collapsible to icon-only mode
    - _Requirements: 9.2, 9.4, 9.6, 9.7_

  - [x] 5.3 Create Header component (`src/components/layout/Header.tsx`)
    - Org name, user avatar dropdown (Profile, Preferences, Sessions, Logout), notification bell icon, global search input
    - Logout action: call `POST /api/v1/auth/logout`, clear auth store, redirect to `/login`
    - _Requirements: 9.3, 6.1_

  - [x] 5.4 Create AuthLayout (`src/components/layout/AuthLayout.tsx`)
    - Centered card layout for login, password reset, invite acceptance pages
    - _Requirements: 8.5_

  - [x] 5.5 Create AdminLayout (`src/components/layout/AdminLayout.tsx`)
    - PlatformAdmin-specific layout with admin sidebar (Organizations) and admin header
    - _Requirements: 9.5_

  - [x] 5.6 Create shared UI components
    - Create `src/components/common/DataTable.tsx` — generic paginated sortable table with skeleton loading
    - Create `src/components/common/Pagination.tsx` — page controls with size selector (10/20/50)
    - Create `src/components/common/Modal.tsx` — accessible dialog with focus trap, Escape to close
    - Create `src/components/common/ConfirmDialog.tsx` — confirmation modal for destructive actions
    - Create `src/components/common/Badge.tsx` — color-coded badge for status/priority/role with correct color mappings
    - Create `src/components/common/SkeletonLoader.tsx` — layout-matching skeleton placeholders (table, card, form, board variants)
    - Create `src/components/common/EmptyState.tsx` — empty data placeholder with optional CTA
    - Create `src/components/common/Toast.tsx` — toast setup via `useToast()` hook, top-right, auto-dismiss 5s (success), manual (error)
    - _Requirements: 40.1, 40.3, 43.2, 43.3, 43.4, 43.5, 39.6, 12.7, 12.8_

  - [x] 5.7 Create shared form components
    - Create `src/components/forms/FormField.tsx` — form field wrapper with label and inline error
    - Create `src/components/forms/PasswordInput.tsx` — password field with visibility toggle and strength indicator
    - Create `src/components/forms/OtpInput.tsx` — 6-digit OTP input with auto-focus advance
    - Create `src/components/forms/MarkdownEditor.tsx` — Markdown textarea with preview toggle
    - Create `src/components/forms/MemberSelector.tsx` — searchable team member dropdown
    - Create `src/components/forms/DatePicker.tsx` — date selection component
    - Create `src/components/forms/ColorPicker.tsx` — color selection for labels and branding
    - _Requirements: 41.1, 41.3, 41.5_

- [x] 6. Checkpoint — Layout and routing verification
  - Ensure routing works, guards redirect correctly, layout renders with sidebar/header, and shared components are functional. Ask the user if questions arise.

- [x] 7. Auth Feature
  - [x] 7.1 Create auth Zod schemas (`src/features/auth/schemas.ts`)
    - `loginSchema` — email format, password non-empty
    - `passwordSchema` — min 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char from `!@#$%^&*`, confirm match
    - `otpSchema` — exactly 6 digits
    - `platformAdminLoginSchema` — username non-empty, password non-empty
    - _Requirements: 2.9, 4.6, 5.8_

  - [x] 7.2 Create LoginPage (`src/features/auth/pages/LoginPage.tsx`)
    - Login form with email and password fields, "Login" button, "Forgot Password?" link
    - Call `securityApi.login()` on submit
    - On success + `isFirstTimeUser=true` → redirect to `/password/change`
    - On success + `isFirstTimeUser=false` → redirect to dashboard `/`
    - Handle error codes: `INVALID_CREDENTIALS` (2001), `ACCOUNT_LOCKED` (2002), `ACCOUNT_INACTIVE` (2003), `SUSPICIOUS_LOGIN` (2017)
    - Disable submit button during API call
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 40.2_

  - [x] 7.3 Create PlatformAdminLoginPage (`src/features/auth/pages/PlatformAdminLoginPage.tsx`)
    - Login form with username and password fields
    - Call `securityApi.login()` with username instead of email
    - On success → redirect to `/admin/organizations`; if `isFirstTimeUser=true` → redirect to `/password/change`
    - Detect PlatformAdmin context from JWT claims (no `organizationId`, `roleName=PlatformAdmin`)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 7.4 Create ForcedPasswordChangePage (`src/features/auth/pages/ForcedPasswordChangePage.tsx`)
    - Form with "New Password" and "Confirm Password" fields with PasswordStrengthIndicator
    - Call `securityApi.forcedPasswordChange()` on submit
    - Handle: `PASSWORD_COMPLEXITY_FAILED` (2018), `PASSWORD_REUSE_NOT_ALLOWED` (2004)
    - On success → redirect to dashboard `/`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [x] 7.5 Create PasswordResetPage (`src/features/auth/pages/PasswordResetPage.tsx`)
    - Multi-step flow: Step 1 — email input → call `securityApi.requestPasswordReset()`; Step 2 — OTP input + new password → call `securityApi.confirmPasswordReset()`
    - Handle: `OTP_EXPIRED` (2007) with "Resend Code" button, `OTP_MAX_ATTEMPTS` (2009), `PASSWORD_RECENTLY_USED` (2005)
    - On success → redirect to `/login` with success toast
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

  - [x] 7.6 Wire auth into App — session restore on load
    - In `App.tsx` or a top-level provider, attempt session restore by calling refresh endpoint if refresh token exists
    - On successful restore, populate auth store and fetch org data
    - _Requirements: 6.4, 6.3_

- [x] 8. Dashboard Feature
  - [x] 8.1 Create DashboardPage (`src/features/dashboard/pages/DashboardPage.tsx`)
    - Render 4 widgets in parallel: SprintProgressWidget, MyTasksWidget, RecentActivityWidget, VelocityChartWidget
    - Each widget fetches independently with its own loading/error state
    - Failed widgets show error state without affecting siblings (error boundary or try/catch)
    - _Requirements: 10.1, 10.6, 10.7_

  - [x] 8.2 Create dashboard widgets
    - `SprintProgressWidget` — call `GET /api/v1/sprints/active`, display sprint name, progress bar, remaining days, total story points
    - `MyTasksWidget` — call `GET /api/v1/stories?assignee={userId}&status=InProgress,InReview`, display assigned stories/tasks with status badges
    - `RecentActivityWidget` — call `GET /api/v1/audit-logs?action=StoryCreated,TaskAssigned,StatusChanged&pageSize=20`, display chronological feed
    - `VelocityChartWidget` — call `GET /api/v1/sprints/velocity?count=10`, render Recharts bar chart
    - _Requirements: 10.2, 10.3, 10.4, 10.5_

- [x] 9. Projects Feature
  - [x] 9.1 Create project Zod schema and form (`src/features/projects/schemas.ts`)
    - `createProjectSchema` — name required, projectKey `^[A-Z0-9]{2,10}$`, description optional, leadId optional UUID
    - _Requirements: 11.7_

  - [x] 9.2 Create ProjectListPage (`src/features/projects/pages/ProjectListPage.tsx`)
    - Call `workApi.getProjects()`, display paginated DataTable with name, project key, story count, sprint count, lead name, status badge
    - "Create Project" button (visible to OrgAdmin, DeptLead) opens ProjectForm modal
    - Handle: `PROJECT_KEY_DUPLICATE` (4043), `PROJECT_NAME_DUPLICATE` (4042)
    - Click row → navigate to `/projects/:id`
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_

  - [x] 9.3 Create ProjectDetailPage (`src/features/projects/pages/ProjectDetailPage.tsx`)
    - Call `workApi.getProject(id)`, display project detail with story list, sprint list, and project settings
    - Edit project form with `workApi.updateProject()`
    - _Requirements: 11.6_

- [x] 10. Stories Feature
  - [x] 10.1 Create story Zod schemas (`src/features/stories/schemas.ts`)
    - `createStorySchema` — projectId UUID, title required max 200, description max 5000, storyPoints Fibonacci, priority enum, labelIds array
    - _Requirements: 14.5_

  - [x] 10.2 Create StoryListPage (`src/features/stories/pages/StoryListPage.tsx`)
    - Call `workApi.getStories()`, display paginated DataTable with Story Key, Title, Status, Priority, Story Points, Assignee, Sprint, Project, Labels
    - Filter controls: project, status (multi-select), priority (multi-select), department, assignee (searchable), sprint, labels, date range
    - Sortable columns, pagination (10/20/50)
    - Status badges with color coding, priority badges with color coding
    - Click row → navigate to `/stories/:id`
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8_

  - [x] 10.3 Create StoryDetailPage (`src/features/stories/pages/StoryDetailPage.tsx`)
    - Call `workApi.getStory(id)`, display full story detail
    - Header: Story Key, Title, Status badge, Priority badge, Story Points, Assignee, Reporter, Project, Sprint, Due Date, dates
    - Description and Acceptance Criteria with Markdown rendering
    - Tasks section with task list and "Create Task" button
    - Completion percentage bar and department contribution breakdown
    - Labels section with colored chips and "Add Label" button
    - Links section with link type badges and "Link Story" button
    - Activity Log timeline from `workApi.getStoryActivity(id)`
    - Comments section (wired in task 14)
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.8, 13.9, 13.10_

  - [x] 10.4 Create StoryForm and story create/edit flow
    - StoryForm component with fields: Project, Title, Description (MarkdownEditor), Acceptance Criteria, Priority, Story Points (Fibonacci dropdown), Department, Due Date, Labels
    - Create: call `workApi.createStory()`, navigate to new story detail, success toast
    - Edit: populate form, call `workApi.updateStory()`
    - Handle: `INVALID_STORY_POINTS` (4023), `INVALID_PRIORITY` (4024)
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.6, 14.7_

  - [x] 10.5 Create StatusTransitionButtons component
    - Render only valid transitions from `workflowStateMachine` for current story status
    - Call `workApi.updateStoryStatus()` on click
    - Handle: `INVALID_STORY_TRANSITION` (4004), `STORY_REQUIRES_ASSIGNEE` (4013), `STORY_REQUIRES_TASKS` (4014), `STORY_REQUIRES_POINTS` (4015)
    - "Assign" button (DeptLead+) with MemberSelector, call `workApi.assignStory()`
    - _Requirements: 13.11, 13.12, 13.13_

  - [x] 10.6 Create LabelManager component
    - "Add Label" dropdown from `workApi.getLabels()` with search filter
    - Apply label: `workApi.applyLabel()`, remove label: `workApi.removeLabel()`
    - "Manage Labels" dialog (DeptLead+): create (name + ColorPicker), edit, delete
    - Handle: `LABEL_NAME_DUPLICATE` (4011)
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_

  - [x] 10.7 Create StoryLinkDialog component
    - Story search field (by key or title) + link type dropdown (blocks, is blocked by, relates to, duplicates)
    - Create link: `workApi.createStoryLink()`, remove link: `workApi.removeStoryLink()`
    - Display linked stories with Story Key (clickable), Title, Link Type badge, remove button
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

  - [x] 10.8 Create StoryByKeyRedirect (`src/features/stories/pages/StoryByKeyRedirect.tsx`)
    - Call `workApi.getStoryByKey(key)` and redirect to `/stories/:id`
    - _Requirements: 8.6_

- [x] 11. Checkpoint — Core features verification
  - Ensure auth flows, dashboard, projects, and stories features work end-to-end. Ask the user if questions arise.

- [x] 12. Tasks Feature
  - [x] 12.1 Create task Zod schema (`src/features/tasks/schemas.ts`)
    - `createTaskSchema` — storyId UUID, title required max 200, description max 3000, taskType enum required, priority enum, estimatedHours positive, dueDate
    - _Requirements: 17.11_

  - [x] 12.2 Create TaskForm component (`src/features/tasks/components/TaskForm.tsx`)
    - Fields: Title, Description, Task Type (dropdown: Development/Testing/DevOps/Design/Documentation/Bug), Priority, Estimated Hours, Due Date
    - Create: call `workApi.createTask()` with parent `storyId`
    - _Requirements: 17.1, 17.2_

  - [x] 12.3 Create TaskDetailPanel (`src/features/tasks/components/TaskDetailPanel.tsx`)
    - Slide-over or modal showing: Title, Description, Status, Task Type, Priority, Assignee, Department (auto-mapped), Estimated Hours, Actual Hours, Due Date, Comments, Activity Log
    - Workflow transition buttons: ToDo→InProgress (requires assignee), InProgress→InReview, InReview→InProgress, InReview→Done
    - Call `workApi.updateTaskStatus()` on transition
    - Handle: `INVALID_TASK_TRANSITION` errors
    - _Requirements: 17.3, 17.4, 17.5_

  - [x] 12.4 Create task assignment and time logging
    - "Assign" button (DeptLead+) with MemberSelector filtered to task's department, call `workApi.assignTask()`
    - "Self-Assign" button (Member+), call `workApi.selfAssignTask()`
    - "Suggest Assignee" button, call `workApi.suggestAssignee()`, pre-fill selector
    - Handle: `ASSIGNEE_NOT_IN_DEPARTMENT` (4018), `ASSIGNEE_AT_CAPACITY` (4019)
    - "Log Hours" dialog with hours (positive number) and description, call `workApi.logHours()`
    - _Requirements: 17.6, 17.7, 17.8, 17.9, 17.10_

- [x] 13. Sprints Feature
  - [x] 13.1 Create sprint Zod schema (`src/features/sprints/schemas.ts`)
    - `createSprintSchema` — name required, goal optional, startDate required, endDate required, endDate after startDate
    - _Requirements: 18.9_

  - [x] 13.2 Create SprintListPage (`src/features/sprints/pages/SprintListPage.tsx`)
    - Call `workApi.getSprints()`, display list with Sprint Name, Project, Status badge, Start/End Date, Story Count, Velocity, Progress bar
    - "Create Sprint" button (OrgAdmin, DeptLead) opens SprintForm
    - Project dropdown filter
    - Sprint lifecycle buttons: "Start Sprint" (Planning→Active), "Complete Sprint" (Active→Completed), "Cancel Sprint" (Active→Cancelled)
    - Handle: `SPRINT_END_BEFORE_START` (4033), `ONLY_ONE_ACTIVE_SPRINT` (4016)
    - "Complete Sprint" shows confirmation dialog with incomplete stories info
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8_

  - [x] 13.3 Create SprintDetailPage with planning view (`src/features/sprints/pages/SprintDetailPage.tsx`)
    - Call `workApi.getSprint(id)` and `workApi.getSprintMetrics(id)`
    - For Planning status: render SprintPlanningView — split view with backlog (left) and sprint stories (right)
    - dnd-kit drag-and-drop: backlog→sprint calls `workApi.addStoryToSprint()`, sprint→backlog calls `workApi.removeStoryFromSprint()`
    - Display total story points and story count in sprint panel header, update in real-time
    - Handle: `STORY_ALREADY_IN_SPRINT` (4007), `SPRINT_NOT_IN_PLANNING` (4006), `STORY_PROJECT_MISMATCH` (4046)
    - _Requirements: 19.1, 19.2, 19.3, 19.4, 19.5, 19.6, 19.7, 19.8_

  - [x] 13.4 Create sprint metrics and burndown chart
    - SprintMetricsPanel: Total Stories, Completed Stories, Total/Completed Story Points, Completion Rate, Velocity, Stories by Status (bar chart), Tasks by Department (bar chart)
    - BurndownChart using Recharts: Ideal Remaining Points (linear line) vs Actual Remaining Points over sprint duration
    - Sprint stories table with status, priority, assignee, story points
    - Auto-refresh metrics every 5 minutes when sprint is Active
    - _Requirements: 20.1, 20.2, 20.3, 20.4, 20.5_

- [x] 14. Boards Feature
  - [x] 14.1 Create board shared components
    - `BoardColumn` — renders column with status header (name, card count, total points)
    - `DraggableCard` — card component for dnd-kit with story/task info
    - `BoardFilters` — filter controls (project, department, assignee, priority, labels)
    - `useBoardDragDrop` hook — dnd-kit sensors, `handleDragEnd` with optimistic update and revert on API failure
    - _Requirements: 21.5, 21.6, 40.5_

  - [x] 14.2 Create KanbanBoardPage (`src/features/boards/pages/KanbanBoardPage.tsx`)
    - Call `workApi.getKanbanBoard()`, render columns: Backlog, Ready, InProgress, InReview, QA, Done, Closed
    - Story cards: Story Key, Title, Priority badge, Story Points, Assignee avatar, Labels (colored dots), Task progress, Project name
    - dnd-kit drag-and-drop between columns → call `workApi.updateStoryStatus()`
    - On failed transition: revert card to original column, show toast
    - Click card → navigate to `/stories/:id`
    - Project filter dropdown at top
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.7, 21.8_

  - [x] 14.3 Create SprintBoardPage (`src/features/boards/pages/SprintBoardPage.tsx`)
    - Call `workApi.getSprintBoard()`, render columns: ToDo, InProgress, InReview, Done
    - Task cards: Task Title, Parent Story Key, Task Type badge, Assignee avatar, Department badge, Priority
    - dnd-kit drag-and-drop → call `workApi.updateTaskStatus()`
    - Project filter and sprint selector at top
    - Active sprint name, remaining days, progress bar in header
    - _Requirements: 22.1, 22.2, 22.3, 22.4, 22.5, 22.6_

  - [x] 14.4 Create DepartmentBoardPage (`src/features/boards/pages/DepartmentBoardPage.tsx`)
    - Call `workApi.getDepartmentBoard()`, render columns per department
    - Task cards: Task Title, Parent Story Key, Status badge, Assignee avatar, Priority
    - Column headers: Department name, Task count, workload indicator
    - Project filter at top
    - _Requirements: 23.1, 23.2, 23.3, 23.4_

  - [x] 14.5 Create BacklogPage (`src/features/boards/pages/BacklogPage.tsx`)
    - Call `workApi.getBacklog()`, display stories not in sprint sorted by priority (Critical first)
    - Story rows: Story Key, Title, Priority badge, Story Points, Assignee, Labels, Created Date
    - Project filter at top
    - Drag-and-drop reordering by priority
    - "Add to Sprint" action with sprint selector (Planning status sprints)
    - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.5_

- [x] 15. Checkpoint — Work management features verification
  - Ensure tasks, sprints, and all four board views work with drag-and-drop. Ask the user if questions arise.

- [x] 16. Comments Feature
  - [x] 16.1 Create CommentSection and related components
    - `CommentSection` — fetches comments via `workApi.getComments(entityType, entityId)`, renders threaded list
    - `CommentItem` — author name, avatar, timestamp, Markdown-rendered content, Edit/Delete actions (Edit: author only, Delete: author + OrgAdmin)
    - `CommentInput` — Markdown support (bold, italic, code, links, lists) with preview toggle
    - `MentionAutocomplete` — on `@` trigger, show team member dropdown from `profileApi.getTeamMembers()`, filter by typed text
    - Create comment: `workApi.createComment()` with entityType, entityId, content, optional parentCommentId
    - Reply: nested input with parentCommentId
    - Edit: inline editable input, call `workApi.updateComment()`
    - Delete: confirmation dialog, call `workApi.deleteComment()`
    - Handle: `COMMENT_NOT_AUTHOR` (4017), `MENTION_USER_NOT_FOUND` (4029)
    - _Requirements: 25.1, 25.2, 25.3, 25.4, 25.5, 25.6, 25.7, 25.8, 25.9_

  - [x] 16.2 Wire CommentSection into StoryDetailPage and TaskDetailPanel
    - Add `<CommentSection entityType="Story" entityId={storyId} />` to StoryDetailPage
    - Add `<CommentSection entityType="Task" entityId={taskId} />` to TaskDetailPanel
    - _Requirements: 13.7, 17.3_

- [x] 17. Members and Departments Features
  - [x] 17.1 Create MemberListPage (`src/features/members/pages/MemberListPage.tsx`)
    - Call `profileApi.getTeamMembers()`, display paginated list: Name, Professional ID, Email, Department(s), Role(s), Availability badge, Status badge
    - Filter controls: Department, Role, Status, Availability
    - Click row → navigate to `/members/:id`
    - _Requirements: 26.1, 26.2_

  - [x] 17.2 Create MemberProfilePage (`src/features/members/pages/MemberProfilePage.tsx`)
    - Call `profileApi.getTeamMember(id)`, display full profile: department memberships with roles, skills, availability, MaxConcurrentTasks, active task count, recent activity
    - OrgAdmin management actions: Change Role (per department), Add to Department, Remove from Department, Change Status
    - "Change Role" dialog → `profileApi.changeRole()`
    - "Add to Department" dialog → `profileApi.addToDepartment()`
    - Handle: `MEMBER_ALREADY_IN_DEPARTMENT` (3011), `LAST_ORGADMIN_CANNOT_DEACTIVATE` (3004)
    - CapacityBar: current task load vs MaxConcurrentTasks as progress bar
    - Self-edit: Availability, MaxConcurrentTasks, profile fields
    - _Requirements: 26.3, 26.4, 26.5, 26.6, 26.7, 26.8, 26.9, 26.10_

  - [x] 17.3 Create DepartmentListPage (`src/features/departments/pages/DepartmentListPage.tsx`)
    - Call `profileApi.getDepartments()`, display list: Name, Code, Member Count, Is Default badge, Status
    - "Create Department" button (OrgAdmin) opens DepartmentForm
    - Handle: `DEPARTMENT_NAME_DUPLICATE` (3008), `DEPARTMENT_CODE_DUPLICATE` (3009)
    - Click row → navigate to `/departments/:id`
    - _Requirements: 27.1, 27.2, 27.3, 27.4, 27.5_

  - [x] 17.4 Create DepartmentDetailPage (`src/features/departments/pages/DepartmentDetailPage.tsx`)
    - Display department details, member list with roles
    - DepartmentPreferencesForm (DeptLead/OrgAdmin): Default Task Types, WIP Limit Per Status, Default Assignee, Notification Channel Overrides, Max Concurrent Tasks Default
    - Call `profileApi.getDepartmentPreferences()` and `profileApi.updateDepartmentPreferences()`
    - Default departments: disable delete, show "Default departments cannot be deleted"
    - _Requirements: 27.6, 27.7, 45.1, 45.2, 45.3_

- [x] 18. Settings and Preferences Features
  - [x] 18.1 Create SettingsPage (`src/features/settings/pages/SettingsPage.tsx`)
    - Call `profileApi.getOrganization(id)` and `profileApi.getOrganizationSettings(id)`
    - Render settings grouped by category: General, Workflow, Board, Notification, Data
    - All fields per design: Story ID Prefix, Timezone, Default Sprint Duration, Working Days, Working Hours, Logo URL, Primary Color, Story Point Scale, Auto-Assignment, Board defaults, WIP Limits, Notification channels, Digest Frequency, Audit Retention Days
    - Submit: `profileApi.updateOrganizationSettings()`
    - Handle: `STORY_PREFIX_INVALID_FORMAT` (3020), `STORY_PREFIX_DUPLICATE` (3006), `STORY_PREFIX_IMMUTABLE` (3007)
    - Client-side validation with orgSettingsSchema
    - _Requirements: 28.1, 28.2, 28.3, 28.4, 28.5, 28.6, 28.7_

  - [x] 18.2 Create PreferencesPage (`src/features/preferences/pages/PreferencesPage.tsx`)
    - Call `profileApi.getPreferences()`, render preference fields: Theme, Language, Timezone Override, Default Board View, Email Digest Frequency, Keyboard Shortcuts Enabled, Date Format, Time Format
    - On change: `profileApi.updatePreferences()`
    - Theme change: immediately apply via themeStore without page reload
    - Handle: `INVALID_PREFERENCE_VALUE` (3026)
    - ResolvedPreferencesView: call `profileApi.getResolvedPreferences()`, display effective preferences with cascade level indicators
    - _Requirements: 29.1, 29.2, 29.3, 29.4, 29.5, 29.6_

- [x] 19. Invitations Feature
  - [x] 19.1 Create InviteManagementPage (`src/features/invites/pages/InviteManagementPage.tsx`)
    - Call `profileApi.getInvites()`, display list: Email, Name, Department, Role, Expiry Date, Status
    - "Create Invite" button opens InviteForm: Email, First Name, Last Name, Department (OrgAdmin sees all, DeptLead sees own), Role
    - Submit: `profileApi.createInvite()`
    - "Cancel Invite" with confirmation: `profileApi.cancelInvite()`
    - Handle: `INVITE_EMAIL_ALREADY_MEMBER` (3014)
    - Client-side validation with createInviteSchema
    - _Requirements: 31.1, 31.2, 31.3, 31.4, 31.5, 31.6_

  - [x] 19.2 Create AcceptInvitePage (`src/features/invites/pages/AcceptInvitePage.tsx`)
    - Public page (no auth), call `profileApi.validateInvite(token)`, display org name, department, role
    - OTP verification form, call `profileApi.acceptInvite(token, { otp })`
    - On success → redirect to `/login` with success toast
    - Handle: `INVITE_EXPIRED_OR_INVALID` (3002)
    - _Requirements: 32.1, 32.2, 32.3, 32.4, 32.5_

- [x] 20. Sessions and Devices Feature
  - [x] 20.1 Create SessionManagementPage (`src/features/sessions/pages/SessionManagementPage.tsx`)
    - Call `profileApi.getDevices()` and `securityApi.getSessions()`
    - Devices section: Device Name, Device Type (icon), Is Primary badge, IP Address, Last Active Date, "Remove" button, "Set as Primary" button
    - Sessions section: Device info, IP Address, Created timestamp, "Revoke" button
    - "Revoke All Other Sessions" button → `securityApi.revokeAllSessions()`
    - "Revoke" → `securityApi.revokeSession()` with confirmation
    - "Set as Primary" → `profileApi.setPrimaryDevice()`
    - "Remove" → `profileApi.removeDevice()` with confirmation
    - Handle: `MAX_DEVICES_REACHED` (3003)
    - _Requirements: 33.1, 33.2, 33.3, 33.4, 33.5, 33.6, 33.7, 33.8_

- [x] 21. Search Feature
  - [x] 21.1 Create SearchPage (`src/features/search/pages/SearchPage.tsx`)
    - Read query from URL `?q={query}`, call `workApi.search()`
    - Display results grouped by type (Stories, Tasks): Story Key/Task Title, Status badge, Priority badge, Assignee, matching text snippet
    - Filter controls: Type (Stories/Tasks/All), Project, Status, Priority, Date Range
    - Minimum 2 characters validation — show message and skip API call if less
    - Debounce search input by 300ms via `useDebounce`
    - Click result → navigate to `/stories/:id` or task detail
    - _Requirements: 35.1, 35.2, 35.3, 35.4, 35.5, 35.6_

  - [x] 21.2 Wire global search input in Header
    - Header search input: on Enter or click → navigate to `/search?q={query}`
    - _Requirements: 35.1_

- [x] 22. Reports Feature
  - [x] 22.1 Create ReportsPage (`src/features/reports/pages/ReportsPage.tsx`)
    - Reports dashboard with tabs/cards: Velocity, Department Workload, Capacity Utilization, Cycle Time, Task Completion Rate
    - Date range and project filters on all reports
    - _Requirements: 37.1, 37.7_

  - [x] 22.2 Create report chart components
    - `VelocityChart` — `workApi.getVelocityReport()`, Recharts bar chart with velocity per sprint + average trend line, adjustable sprint count (5/10/15/20)
    - `DepartmentWorkloadChart` — `workApi.getDepartmentWorkloadReport()`, stacked bar chart of task distribution across departments
    - `CapacityUtilizationChart` — `workApi.getCapacityReport()`, chart showing member utilization per department
    - `CycleTimeChart` — `workApi.getCycleTimeReport()`, line chart of average creation-to-completion time
    - `TaskCompletionChart` — `workApi.getTaskCompletionReport()`, chart of completion rates by department and task type
    - _Requirements: 37.2, 37.3, 37.4, 37.5, 37.6, 46.1, 46.2, 46.3, 46.4_

- [x] 23. Saved Filters Feature
  - [x] 23.1 Create SaveFilterDialog and SavedFilterDropdown
    - `SaveFilterDialog` — "Save Filter" button on board/search pages when filters active, name input, call `workApi.createSavedFilter()`
    - `SavedFilterDropdown` — dropdown listing saved filters from `workApi.getSavedFilters()`, apply filter on select, "Delete" action with confirmation → `workApi.deleteSavedFilter()`
    - Wire into KanbanBoardPage, SprintBoardPage, DepartmentBoardPage, BacklogPage, and SearchPage
    - _Requirements: 36.1, 36.2, 36.3, 36.4, 36.5_

- [x] 24. Checkpoint — Secondary features verification
  - Ensure comments, members, departments, settings, preferences, invitations, sessions, search, reports, and saved filters work. Ask the user if questions arise.

- [x] 25. PlatformAdmin Feature
  - [x] 25.1 Create PlatformAdminOrganizationsPage (`src/features/admin/pages/PlatformAdminOrganizationsPage.tsx`)
    - Call `profileApi.getAllOrganizations()`, display list: Name, Status, Member Count, Created Date
    - "Create Organization" form: Organization Name, Description, Website, Story ID Prefix → `profileApi.createOrganization()`
    - "Provision Admin" form: Email, First Name, Last Name → `profileApi.provisionAdmin(orgId, data)`
    - Organization status actions: Activate, Suspend, Deactivate
    - Handle: `ORGANIZATION_NAME_DUPLICATE` (3005)
    - _Requirements: 38.1, 38.2, 38.3, 38.4, 38.5, 38.6_

- [x] 26. Notification Preferences and History
  - [x] 26.1 Create NotificationPreferencesTable (`src/features/preferences/components/NotificationPreferencesTable.tsx`)
    - Call `profileApi.getNotificationSettings()`, render table: rows for 8 notification types (StoryAssigned, TaskAssigned, SprintStarted, SprintEnded, MentionedInComment, StoryStatusChanged, TaskStatusChanged, DueDateApproaching), columns for Email/Push/InApp as toggle switches
    - On toggle change: `profileApi.updateNotificationSetting(typeId, data)`
    - Human-readable type names
    - Wire into PreferencesPage
    - _Requirements: 34.1, 34.2, 34.3, 34.4_

  - [x] 26.2 Create notification history UI
    - Notification bell dropdown in Header: recent notifications from `utilityApi.getNotificationLogs()`, type icon, subject, timestamp, status
    - Unread count badge on bell icon
    - "View All" link to full notification history page with pagination and filters (type, channel, status, date range)
    - _Requirements: 48.1, 48.2, 48.3, 48.4_

- [x] 27. Activity Log Components
  - [x] 27.1 Create ActivityLog component
    - Reusable timeline component for story and task detail pages
    - Render each entry: Actor name, Action description, Old Value → New Value, Timestamp
    - Icons per activity type: status change (arrow), assignment (person), comment (chat bubble), label change (tag), edit (pencil)
    - Wire into StoryDetailPage (`workApi.getStoryActivity()`) and TaskDetailPanel (`workApi.getTaskActivity()`)
    - _Requirements: 47.1, 47.2, 47.3, 47.4_

- [x] 28. Checkpoint — All features verification
  - Ensure PlatformAdmin, notification preferences/history, and activity logs work. Ask the user if questions arise.

- [x] 29. Testing (optional)
  - [x] 29.1 Write property test: Request interceptor attaches required headers
    - **Property 1: Request interceptor attaches required headers**
    - For any outgoing request with access token, verify Authorization and X-Correlation-Id headers
    - **Validates: Requirements 1.2, 1.6**

  - [x] 29.2 Write property test: ApiResponse parsing extracts data or throws typed error
    - **Property 2: ApiResponse parsing extracts data or throws typed error**
    - For any ApiResponse, success=true returns data, success=false throws ApiError
    - **Validates: Requirements 1.4**

  - [x] 29.3 Write property test: Login form validation schema
    - **Property 3: Login form validation schema**
    - For any (email, password) pair, schema accepts iff email is valid format and password is non-empty
    - **Validates: Requirements 2.9**

  - [x] 29.4 Write property test: JWT decode extracts user claims
    - **Property 4: JWT decode extracts user claims**
    - For any valid JWT payload, decode produces correct AuthUser; PlatformAdmin detection when no organizationId
    - **Validates: Requirements 7.3, 3.5**

  - [x] 29.5 Write property test: Password complexity validation schema
    - **Property 5: Password complexity validation schema**
    - For any string, schema accepts iff ≥8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char
    - **Validates: Requirements 4.6**

  - [x] 29.6 Write property test: OTP validation schema
    - **Property 6: OTP validation schema**
    - For any string, schema accepts iff exactly 6 digit characters
    - **Validates: Requirements 5.8**

  - [x] 29.7 Write property test: Auth store state consistency
    - **Property 8: Auth store state consistency**
    - For any login/logout sequence, verify isAuthenticated, tokens, and user state
    - **Validates: Requirements 7.2**

  - [x] 29.8 Write property test: Workflow state machine valid transitions
    - **Property 15: Workflow state machine valid transitions**
    - For any entity type and status, getValidTransitions returns only defined transitions; isValidTransition is correct
    - **Validates: Requirements 13.11, 17.4**

  - [x] 29.9 Write property test: Error code to user message mapping
    - **Property 19: Error code to user message mapping**
    - For any known error code, mapErrorCode returns correct message; unknown codes return fallback
    - **Validates: Requirements 39.2**

  - [x] 29.10 Write property test: Theme store resolves and applies theme correctly
    - **Property 21: Theme store resolves and applies theme correctly**
    - For any theme setting, resolvedTheme is correct; dark class on html iff resolvedTheme=Dark
    - **Validates: Requirements 29.4, 30.2, 30.4**

  - [x] 29.11 Write property test: Zod form validation schemas
    - **Property 16: Zod form validation schemas accept valid and reject invalid inputs**
    - For all form schemas (createStory, createTask, createSprint, createProject, createInvite, orgSettings), valid inputs parse, invalid inputs produce errors
    - **Validates: Requirements 14.5, 17.11, 18.9, 11.7, 28.7, 31.6**

  - [x] 29.12 Write property test: Search minimum query length enforcement
    - **Property 22: Search minimum query length enforcement**
    - For any query <2 chars, no API call; for ≥2 chars, search proceeds
    - **Validates: Requirements 35.4**

  - [x] 29.13 Write unit tests for route guards
    - Test AuthGuard redirects unauthenticated users to /login and stores intended destination
    - Test RoleGuard redirects unauthorized users to / with toast
    - Test GuestGuard redirects authenticated users to /
    - Test FirstTimeGuard redirects non-first-time users to /
    - **Validates: Properties 9, 10, 11; Requirements 8.2, 8.3, 8.4, 4.7**

  - [x] 29.14 Write unit tests for Badge color mapping
    - Test status Badge renders correct colors for all StoryStatus values
    - Test priority Badge renders correct colors for all Priority values
    - **Validates: Property 14; Requirements 12.7, 12.8**

  - [ ]* 29.15 Write unit tests for board drag-and-drop optimistic revert
    - Test that on API failure, card reverts to original column and error toast is shown
    - **Validates: Property 18; Requirements 21.4, 40.5**

- [x] 30. Final Verification
  - Ensure all features are wired together, all routes are accessible, all error codes are handled, and the application compiles without errors. Run `npm run build` to verify production build. Ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties using fast-check
- Unit tests validate specific examples and edge cases
- All code lives under `src/frontend/` with feature-based module organization
