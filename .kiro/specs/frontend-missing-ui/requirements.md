# Requirements Document

## Introduction

The Nexus 2.0 frontend (React + TypeScript, Vite, Tailwind CSS) has several gaps where backend API endpoints exist but the UI does not expose them. This feature closes those gaps by adding missing API client methods, UI components, and page-level wiring for: member status management, story/task deletion, story/task unassignment, and standalone OTP request/verify endpoints in the security API client. Comment editing and deletion are already fully implemented and are excluded from this scope.

## Glossary

- **Frontend**: The React + TypeScript single-page application served by Vite, located under `src/frontend/`.
- **API_Client**: A TypeScript module in `src/frontend/src/api/` that wraps Axios calls to a specific backend service (e.g., `workApi.ts`, `profileApi.ts`, `securityApi.ts`).
- **MemberProfilePage**: The page component at `src/frontend/src/features/members/pages/MemberProfilePage.tsx` that displays a team member's profile.
- **StoryDetailPage**: The page component at `src/frontend/src/features/stories/pages/StoryDetailPage.tsx` that displays a story's details, tasks, comments, and activity.
- **TaskRow**: The inline task display component rendered within StoryDetailPage and board views.
- **ConfirmDialog**: The existing reusable confirmation modal at `src/frontend/src/components/common/ConfirmDialog.tsx`.
- **Badge**: The existing reusable badge component at `src/frontend/src/components/common/Badge.tsx`.
- **Toast**: The existing toast notification system accessed via the `useToast` hook.
- **OrgAdmin**: An organization administrator role that has elevated permissions including member status management and deletion of stories/tasks.
- **DeptLead**: A department lead role that has permissions to delete stories and tasks within their department scope.
- **Member_Status**: A single-character flag representing a team member's account status: "A" (Active), "S" (Suspended), "D" (Deactivated).
- **Availability**: A string value representing a team member's current working availability (e.g., Available, Busy, Away, Offline).
- **Soft_Delete**: A deletion operation that marks a record as deleted without physically removing it from the database.

## Requirements

### Requirement 1: OTP Request and Verify API Client Methods

**User Story:** As a developer, I want `securityApi` to expose `requestOtp` and `verifyOtp` methods, so that future features (e.g., two-factor authentication, sensitive-action confirmation) can call the OTP endpoints without duplicating HTTP logic.

#### Acceptance Criteria

1. THE API_Client (`securityApi.ts`) SHALL expose a `requestOtp` method that sends a POST request to `/api/v1/auth/otp/request` with an email payload and returns a Promise resolving to void.
2. THE API_Client (`securityApi.ts`) SHALL expose a `verifyOtp` method that sends a POST request to `/api/v1/auth/otp/verify` with an email and OTP code payload and returns a Promise resolving to a verification result.
3. IF the OTP request endpoint returns an error, THEN THE API_Client SHALL propagate the error as an `ApiError` instance with the backend-provided error code.
4. IF the OTP verify endpoint returns an error, THEN THE API_Client SHALL propagate the error as an `ApiError` instance with the backend-provided error code.

### Requirement 2: Member Status Management

**User Story:** As an OrgAdmin, I want to activate, suspend, or deactivate team members from the MemberProfilePage, so that I can manage member access without using backend tools.

#### Acceptance Criteria

1. THE API_Client (`profileApi.ts`) SHALL expose an `updateMemberStatus` method that sends a PATCH request to `/api/v1/team-members/{id}/status` with a status value ("A", "S", or "D") and returns a Promise.
2. WHEN the MemberProfilePage loads for any member, THE MemberProfilePage SHALL display the member's current status as a color-coded Badge showing "Active", "Suspended", or "Deactivated".
3. WHILE the authenticated user has the OrgAdmin role, THE MemberProfilePage SHALL display a status management control (dropdown or button group) allowing selection of Active, Suspended, or Deactivated statuses.
4. WHEN an OrgAdmin selects a new status value from the status management control, THE MemberProfilePage SHALL display a ConfirmDialog asking the OrgAdmin to confirm the status change before sending the request.
5. WHEN the OrgAdmin confirms the status change, THE MemberProfilePage SHALL call `profileApi.updateMemberStatus` with the member ID and selected status value, then refresh the member data on success.
6. WHEN the status change succeeds, THE MemberProfilePage SHALL display a success Toast notification indicating the member's status has been updated.
7. IF the status change request fails, THEN THE MemberProfilePage SHALL display an error Toast notification with the mapped error message.
8. WHILE the authenticated user does not have the OrgAdmin role, THE MemberProfilePage SHALL hide the status management control.

### Requirement 3: Member Availability API Method

**User Story:** As a developer, I want `profileApi` to expose a dedicated `updateAvailability` method, so that availability changes can be made via the specific PATCH endpoint rather than the general PUT update.

#### Acceptance Criteria

1. THE API_Client (`profileApi.ts`) SHALL expose an `updateAvailability` method that sends a PATCH request to `/api/v1/team-members/{id}/availability` with an availability value and returns a Promise.
2. IF the availability update endpoint returns an error, THEN THE API_Client SHALL propagate the error as an `ApiError` instance with the backend-provided error code.

### Requirement 4: Story Deletion

**User Story:** As a DeptLead or OrgAdmin, I want to delete a story from the StoryDetailPage, so that I can remove obsolete or erroneous stories.

#### Acceptance Criteria

1. THE API_Client (`workApi.ts`) SHALL expose a `deleteStory` method that sends a DELETE request to `/api/v1/stories/{id}` and returns a Promise resolving to void.
2. WHILE the authenticated user has the DeptLead or OrgAdmin role, THE StoryDetailPage SHALL display a "Delete" button in the story header area.
3. WHEN a DeptLead or OrgAdmin clicks the Delete button on the StoryDetailPage, THE StoryDetailPage SHALL display a ConfirmDialog warning that the story will be soft-deleted.
4. WHEN the user confirms deletion in the ConfirmDialog, THE StoryDetailPage SHALL call `workApi.deleteStory` with the story ID.
5. WHEN the deletion succeeds, THE StoryDetailPage SHALL display a success Toast notification and navigate the user back to the story list page (`/stories`).
6. IF the deletion request fails, THEN THE StoryDetailPage SHALL display an error Toast notification with the mapped error message and keep the user on the current page.
7. WHILE the authenticated user does not have the DeptLead or OrgAdmin role, THE StoryDetailPage SHALL hide the Delete button.

### Requirement 5: Task Deletion

**User Story:** As a DeptLead or OrgAdmin, I want to delete a task from the task list or task detail view, so that I can remove incorrect or duplicate tasks.

#### Acceptance Criteria

1. THE API_Client (`workApi.ts`) SHALL expose a `deleteTask` method that sends a DELETE request to `/api/v1/tasks/{id}` and returns a Promise resolving to void.
2. WHILE the authenticated user has the DeptLead or OrgAdmin role, THE TaskRow component SHALL display a delete icon button for each task.
3. WHEN a DeptLead or OrgAdmin clicks the delete icon on a TaskRow, THE parent component SHALL display a ConfirmDialog warning that the task will be soft-deleted.
4. WHEN the user confirms deletion in the ConfirmDialog, THE parent component SHALL call `workApi.deleteTask` with the task ID.
5. WHEN the task deletion succeeds, THE parent component SHALL display a success Toast notification and refresh the task list (or story detail) to reflect the removal.
6. IF the task deletion request fails, THEN THE parent component SHALL display an error Toast notification with the mapped error message.
7. WHILE the authenticated user does not have the DeptLead or OrgAdmin role, THE TaskRow component SHALL hide the delete icon button.

### Requirement 6: Story Unassignment

**User Story:** As a team member with assign permissions, I want to unassign a story's current assignee, so that the story returns to an unassigned state when the assignee is no longer appropriate.

#### Acceptance Criteria

1. THE API_Client (`workApi.ts`) SHALL expose an `unassignStory` method that sends a PATCH request to `/api/v1/stories/{id}/unassign` and returns a Promise resolving to void.
2. WHEN a story has an assignee, THE StoryDetailPage assignee section SHALL display an "Unassign" option (button or dropdown item).
3. WHEN the user clicks the Unassign option for a story, THE StoryDetailPage SHALL call `workApi.unassignStory` with the story ID.
4. WHEN the unassignment succeeds, THE StoryDetailPage SHALL display a success Toast notification and refresh the story data to show "Unassigned" in the assignee field.
5. IF the unassignment request fails, THEN THE StoryDetailPage SHALL display an error Toast notification with the mapped error message.

### Requirement 7: Task Unassignment

**User Story:** As a team member with assign permissions, I want to unassign a task's current assignee, so that the task returns to an unassigned state.

#### Acceptance Criteria

1. THE API_Client (`workApi.ts`) SHALL expose an `unassignTask` method that sends a PATCH request to `/api/v1/tasks/{id}/unassign` and returns a Promise resolving to void.
2. WHEN a task has an assignee, THE task detail or assignment UI SHALL display an "Unassign" option (button or dropdown item).
3. WHEN the user clicks the Unassign option for a task, THE task UI SHALL call `workApi.unassignTask` with the task ID.
4. WHEN the task unassignment succeeds, THE task UI SHALL display a success Toast notification and refresh the task data to show "Unassigned" in the assignee field.
5. IF the task unassignment request fails, THEN THE task UI SHALL display an error Toast notification with the mapped error message.
