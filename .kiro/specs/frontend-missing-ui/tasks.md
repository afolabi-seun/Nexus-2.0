# Implementation Plan: Frontend Missing UI

## Overview

Incrementally add missing API client methods, member status management UI, story/task deletion, and story/task unassignment to the React + TypeScript frontend. Each group is followed by a build checkpoint to catch issues early.

## Tasks

- [x] 1. API client additions
  - [x] 1.1 Add `requestOtp` and `verifyOtp` methods to `securityApi.ts`
    - Add `requestOtp` method: POST to `/api/v1/auth/otp/request` with `{ email }`, returns `Promise<void>`
    - Add `verifyOtp` method: POST to `/api/v1/auth/otp/verify` with `{ email, otp }`, returns `Promise<{ verified: boolean }>`
    - Both methods follow existing `client.post(...).then(...)` pattern
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 1.2 Add `updateMemberStatus` and `updateAvailability` methods to `profileApi.ts`
    - Add `updateMemberStatus` method: PATCH to `/api/v1/team-members/${id}/status` with `{ status }`, returns `Promise<void>`
    - Add `updateAvailability` method: PATCH to `/api/v1/team-members/${id}/availability` with `{ availability }`, returns `Promise<void>`
    - _Requirements: 2.1, 3.1, 3.2_

  - [x] 1.3 Add `deleteStory`, `deleteTask`, `unassignStory`, and `unassignTask` methods to `workApi.ts`
    - Add `deleteStory`: DELETE `/api/v1/stories/${id}`, returns `Promise<void>`
    - Add `deleteTask`: DELETE `/api/v1/tasks/${id}`, returns `Promise<void>`
    - Add `unassignStory`: PATCH `/api/v1/stories/${id}/unassign`, returns `Promise<void>`
    - Add `unassignTask`: PATCH `/api/v1/tasks/${id}/unassign`, returns `Promise<void>`
    - Check that none of these already exist before adding
    - _Requirements: 4.1, 5.1, 6.1, 7.1_

  - [ ]* 1.4 Write property tests for API client methods
    - **Property 1: API client methods produce correct HTTP requests**
    - **Validates: Requirements 1.1, 1.2, 2.1, 3.1, 4.1, 5.1, 6.1, 7.1**
    - Use fast-check with `fc.uuid()` for IDs, `fc.constantFrom('A','S','D')` for status, `fc.emailAddress()` for emails
    - Mock axios and assert correct HTTP method, URL path, and request body for each new method

  - [ ]* 1.5 Write property test for API client error propagation
    - **Property 2: API client error propagation**
    - **Validates: Requirements 1.3, 1.4, 3.2**
    - Generate random error codes, mock axios to reject, assert each method rejects with `ApiError` containing the correct error code

- [x] 2. Checkpoint — API client additions
  - Ensure all tests pass, ask the user if questions arise.
  - Run `npm run build` (or equivalent) to verify no type errors in the API layer changes

- [ ] 3. MemberProfilePage — status management UI
  - [x] 3.1 Add status management dropdown and confirm dialog to `MemberProfilePage.tsx`
    - Import `ConfirmDialog` component
    - Add state: `statusConfirmOpen: boolean`, `pendingStatus: FlgStatus | null`
    - Add OrgAdmin-only `<select>` dropdown with options: Active (A), Suspended (S), Deactivated (D)
    - On dropdown change → set `pendingStatus`, open `ConfirmDialog`
    - On confirm → call `profileApi.updateMemberStatus(id, { status: pendingStatus })`, show success toast, re-fetch member
    - On cancel → reset `pendingStatus`, close dialog
    - On error → show error toast via `mapErrorCode(err.errorCode)`
    - Hide dropdown when `!isOrgAdmin` (use existing `isOrgAdmin` variable)
    - _Requirements: 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8_

  - [ ]* 3.2 Write property test for status management control visibility
    - **Property 3: Status management control visibility is determined by OrgAdmin role**
    - **Validates: Requirements 2.3, 2.8**
    - Generate random role names via `fc.constantFrom('OrgAdmin','DeptLead','Member','Viewer')`; render MemberProfilePage; assert dropdown present iff OrgAdmin

  - [ ]* 3.3 Write property test for status badge mapping
    - **Property 6: Status badge correctly maps flgStatus to display label**
    - **Validates: Requirements 2.2**
    - Generate `flgStatus` from `{A, S, D}`; assert Badge text is "Active"/"Suspended"/"Deactivated" respectively

- [x] 4. Checkpoint — MemberProfilePage changes
  - Ensure all tests pass, ask the user if questions arise.
  - Run build to verify no type errors in MemberProfilePage changes

- [ ] 5. StoryDetailPage and TaskRow — deletion and unassignment
  - [x] 5.1 Add delete button and unassign option to `StoryDetailPage.tsx`
    - Import `ConfirmDialog`, `Trash2`, `UserX` from lucide-react, and `useAuth`
    - Add state: `deleteConfirmOpen: boolean`, `unassigning: boolean`
    - Compute `canDelete = user?.roleName === 'OrgAdmin' || user?.roleName === 'DeptLead'`
    - Add `Trash2` icon button in story header (next to Edit), visible only when `canDelete`
    - On click → open `ConfirmDialog` with soft-delete warning
    - On confirm → call `workApi.deleteStory(id)`, toast success, `navigate('/stories')`
    - On error → toast error via `mapErrorCode`, stay on page
    - Add `UserX` icon button / "Unassign" link next to Assignee MetaItem, visible when `story.assigneeId` is not null
    - On click → call `workApi.unassignStory(id)`, toast success, re-fetch story
    - On error → toast error via `mapErrorCode`
    - _Requirements: 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 6.2, 6.3, 6.4, 6.5_

  - [x] 5.2 Enhance `TaskRow` component with delete and unassign controls
    - Update `TaskRow` props to accept `canDelete`, `onDelete`, `onUnassign`
    - Add `Trash2` icon (size 14) visible when `canDelete`, calls `onDelete(task.taskId)`
    - Add `UserX` icon (size 14) visible when `task.assigneeId` is not null, calls `onUnassign(task.taskId)`
    - In StoryDetailPage, add task deletion confirm dialog state and handlers
    - On task delete confirm → call `workApi.deleteTask(taskId)`, toast success, re-fetch story
    - On task unassign → call `workApi.unassignTask(taskId)`, toast success, re-fetch story
    - Pass `canDelete`, `onDelete`, `onUnassign` props from StoryDetailPage to each `TaskRow`
    - _Requirements: 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 7.2, 7.3, 7.4, 7.5_

  - [ ]* 5.3 Write property test for delete controls visibility
    - **Property 4: Delete controls visibility is determined by DeptLead or OrgAdmin role**
    - **Validates: Requirements 4.2, 4.7, 5.2, 5.7**
    - Generate random role names; render StoryDetailPage/TaskRow; assert delete button/icon present iff OrgAdmin or DeptLead

  - [ ]* 5.4 Write property test for unassign option visibility
    - **Property 5: Unassign option visibility is determined by assignee presence**
    - **Validates: Requirements 6.2, 7.2**
    - Generate random nullable UUIDs for `assigneeId`; render components; assert unassign option present iff non-null

  - [ ]* 5.5 Write property test for error toast messages
    - **Property 7: Error toasts display mapped error messages**
    - **Validates: Requirements 2.7, 4.6, 5.6, 6.5, 7.5**
    - Generate random error codes from `errorCodeMap` keys; simulate failed API calls; assert toast message equals `mapErrorCode(errorCode)`

  - [ ]* 5.6 Write property test for confirm-then-call correctness
    - **Property 8: Confirm action triggers correct API call with correct arguments**
    - **Validates: Requirements 2.5, 4.4, 5.4, 6.3, 7.3**
    - Generate random UUIDs and payloads; simulate confirm flow; assert the correct API method is called exactly once with the correct arguments

- [x] 6. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
  - Run full build to verify no type errors across all changed files

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation after each logical group
- Property tests use fast-check and validate universal correctness properties from the design document
- All UI changes follow existing codebase patterns: `useAuth()` for role checks, `ConfirmDialog` for confirmations, `useToast()` for notifications, `ApiError` + `mapErrorCode()` for error handling
