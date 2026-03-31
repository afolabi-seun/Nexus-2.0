# Implementation Plan: Standardized Filters

## Overview

Replace all ad-hoc filter implementations across 8 list pages with a single, declarative `ListFilter` component and `useListFilters` hook. Create shared types (`FilterConfig`, `FilterValues`, `FilterFieldType`), a `FilterField` renderer component, and the main `ListFilter` component with collapsible bar, saved-filter integration, URL sync, and accessibility. Then migrate each list page incrementally and remove the legacy `BoardFilters` component.

## Tasks

- [ ] 1. Create filter type definitions and useListFilters hook
  - [x] 1.1 Create `src/frontend/src/types/filters.ts` with `FilterFieldType` enum, `FilterConfig` interface, and `FilterValues` type
    - Define `FilterFieldType` as `'select' | 'multi-select' | 'text-search' | 'date' | 'date-range' | 'async-search'`
    - Define `FilterConfig` with fields: `key`, `label`, `type`, `options?`, `loadOptions?`, `placeholder?`
    - Define `FilterValues` as `Record<string, unknown>`
    - _Requirements: 1.1, 1.8_

  - [x] 1.2 Create `src/frontend/src/hooks/useListFilters.ts` hook
    - Accept `FilterConfig[]` and optional `syncToUrl` (default `true`) and `onPageReset` callback
    - Return `filterValues`, `updateFilter`, `clearFilters`, `hasActiveFilters`
    - Implement `updateFilter(key, value)` — set value or remove key when empty/undefined
    - Implement `clearFilters()` — reset to empty record
    - Call `onPageReset` (reset page to 1) on any filter change
    - Debounce `text-search` and `async-search` updates by 300ms
    - Serialize/deserialize filter values to/from URL search params when `syncToUrl` is `true`
    - Skip URL sync when `syncToUrl` is `false`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.2, 3.3, 3.4_

  - [ ] 1.3 Write property tests for useListFilters hook
    - **Property 1: Filter value round-trip** — `updateFilter(key, val)` then reading `filterValues[key]` returns `val`; `updateFilter(key, undefined)` removes the key
    - **Validates: Requirements 2.2, 2.3**
    - **Property 2: Clear filters idempotency** — `clearFilters()` always produces an empty record regardless of prior state
    - **Validates: Requirements 2.4**
    - **Property 3: URL sync round-trip** — serializing `FilterValues` to URL params and deserializing back yields the original values (when `syncToUrl` is `true`)
    - **Validates: Requirements 3.1, 3.2**
    - **Property 4: Page reset on filter change** — every `updateFilter` call triggers `onPageReset`
    - **Validates: Requirements 2.5**

  - [ ] 1.4 Write unit tests for useListFilters hook
    - Test debounce behavior for text-search and async-search fields (300ms delay)
    - Test `syncToUrl: false` keeps URL unchanged
    - Test `hasActiveFilters` returns correct boolean
    - _Requirements: 2.6, 3.3, 3.4_

- [ ] 2. Checkpoint - Verify hook logic
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 3. Create FilterField and ListFilter components
  - [x] 3.1 Create `src/frontend/src/components/common/FilterField.tsx`
    - Render `select` type as single-value dropdown with provided `options`
    - Render `multi-select` type as toggle-pill buttons with `role="group"`, `aria-label`, and `aria-pressed`
    - Render `text-search` type as text input with search icon
    - Render `date` type as single date picker input
    - Render `date-range` type as paired "from" and "to" date inputs
    - Render `async-search` type as text input that calls `loadOptions` after 2+ characters, shows loading indicator while pending, handles errors gracefully
    - For `select` with `loadOptions` (no static `options`), call `loadOptions` once on mount to populate dropdown
    - Associate each input with a visible `<label>` using `htmlFor`/`id`
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 6.1, 6.4, 8.1, 8.2, 8.3, 8.4_

  - [x] 3.2 Create `src/frontend/src/components/common/ListFilter.tsx`
    - Accept `filters: FilterConfig[]`, `filterValues: FilterValues`, `onUpdateFilter`, `onClearFilters`, `hasActiveFilters`, and optional `enableSavedFilters` (default `false`)
    - Render a "Filters" toggle button with `aria-expanded` attribute reflecting open/closed state
    - Show badge count of active filters on the toggle button when >= 1 filter is active
    - Render collapsible filter region with `aria-label="Filter options"` containing a responsive grid (1-col small → 4-col large)
    - Render a FilterField for each FilterConfig entry
    - Show "Clear all" button when at least one filter is active and bar is open
    - When `enableSavedFilters` is `true`, render `SavedFilterDropdown` and `SaveFilterDialog` (reuse existing components)
    - Use same Tailwind design tokens as DataTable (border-input, bg-background, text-foreground, bg-card, border-border)
    - Ensure all interactive elements are keyboard-navigable (Tab, Enter, Space)
    - _Requirements: 1.1, 4.1, 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 6.2, 6.3, 6.5_

  - [ ] 3.3 Write property tests for ListFilter component
    - **Property 5: Active filter count accuracy** — badge count equals the number of non-empty keys in FilterValues
    - **Validates: Requirements 4.3**
    - **Property 6: FilterConfig → rendered fields** — for any valid FilterConfig array, ListFilter renders exactly one FilterField per config entry
    - **Validates: Requirements 1.1**

  - [ ] 3.4 Write unit tests for FilterField component
    - Test each filter type renders the correct input element
    - Test async-search shows loading indicator while pending
    - Test async-search handles loadOptions failure without crashing
    - Test select with loadOptions calls it on mount
    - Test multi-select uses aria-pressed and role="group"
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 6.4, 8.1, 8.2, 8.3, 8.4_

  - [ ] 3.5 Write unit tests for ListFilter component
    - Test toggle button has correct aria-expanded
    - Test filter region has aria-label="Filter options"
    - Test "Clear all" button appears only when filters are active
    - Test SavedFilterDropdown and SaveFilterDialog render when enableSavedFilters is true
    - _Requirements: 4.1, 4.4, 5.2, 5.3, 6.2, 6.3_

- [ ] 4. Checkpoint - Verify components render correctly
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Migrate board pages (BacklogPage, KanbanBoardPage) to ListFilter
  - [x] 5.1 Migrate `src/frontend/src/features/boards/pages/BacklogPage.tsx`
    - Replace `BoardFilters` import and inline `SaveFilterDialog`/`SavedFilterDropdown` with `ListFilter` and `useListFilters`
    - Define FilterConfig for project (`select` with async loadOptions) and priority (`multi-select`)
    - Set `enableSavedFilters: true`
    - Wire `filterValues` into the existing `fetchBacklog` API call
    - _Requirements: 7.4, 5.2, 5.3_

  - [x] 5.2 Migrate `src/frontend/src/features/boards/pages/KanbanBoardPage.tsx`
    - Replace `BoardFilters` import and inline `SaveFilterDialog`/`SavedFilterDropdown` with `ListFilter` and `useListFilters`
    - Define FilterConfig for project (`select` with async loadOptions) and priority (`multi-select`)
    - Set `enableSavedFilters: true`
    - Wire `filterValues` into the existing `fetchBoard` API call
    - _Requirements: 7.5, 5.2, 5.3_

- [ ] 6. Migrate story and sprint pages to ListFilter
  - [x] 6.1 Migrate `src/frontend/src/features/stories/pages/StoryListPage.tsx`
    - Remove all inline filter markup (filter panel, assignee search, toggle pills)
    - Replace with `ListFilter` and `useListFilters`
    - Define FilterConfig entries: project (`select`), status (`multi-select`), priority (`multi-select`), department (`select`), assignee (`async-search`), dateFrom (`date`), dateTo (`date`)
    - Wire `filterValues` into the existing `fetchStories` API call
    - Remove local filter state variables (`filtersVisible`, `assigneeQuery`, `assigneeResults`, etc.)
    - _Requirements: 7.1_

  - [x] 6.2 Migrate `src/frontend/src/features/sprints/pages/SprintListPage.tsx`
    - Remove inline project `<select>` filter
    - Replace with `ListFilter` and `useListFilters`
    - Define FilterConfig for project (`select` with async loadOptions)
    - Wire `filterValues` into the existing `fetchSprints` API call
    - _Requirements: 7.2_

- [ ] 7. Migrate member, department, invite, and admin pages to ListFilter
  - [x] 7.1 Migrate `src/frontend/src/features/members/pages/MemberListPage.tsx`
    - Remove inline `<select>` filters for department, role, status, availability
    - Replace with `ListFilter` and `useListFilters`
    - Define FilterConfig entries: department (`select`), role (`select`), status (`select`), availability (`select`)
    - Wire `filterValues` into the existing `fetchMembers` API call
    - _Requirements: 7.3_

  - [x] 7.2 Migrate `src/frontend/src/features/departments/pages/DepartmentListPage.tsx`
    - Add `ListFilter` and `useListFilters` with a status (`select`) FilterConfig
    - Wire `filterValues` into the existing `fetchDepartments` API call (add status param)
    - _Requirements: 7.7_

  - [x] 7.3 Migrate `src/frontend/src/features/invites/pages/InviteManagementPage.tsx`
    - Add `ListFilter` and `useListFilters` with a status (`select`) FilterConfig
    - Wire `filterValues` into the existing `fetchInvites` API call (add status param)
    - _Requirements: 7.8_

  - [x] 7.4 Migrate `src/frontend/src/features/admin/pages/PlatformAdminBillingPage.tsx`
    - Remove inline status `<select>` and organization search `<input>`
    - Replace with `ListFilter` and `useListFilters`
    - Define FilterConfig entries: status (`select`), organization search (`text-search`)
    - Wire `filterValues` into the existing `fetchSubscriptions` API call
    - _Requirements: 7.6_

- [ ] 8. Checkpoint - Verify all page migrations
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Remove legacy BoardFilters component and clean up
  - [x] 9.1 Delete `src/frontend/src/features/boards/components/BoardFilters.tsx`
    - Verify no remaining imports of `BoardFilters` across the codebase
    - Remove the `BoardFilters` type import from work types if it was only used by the component
    - _Requirements: 7.9_

  - [ ] 9.2 Write property tests for page-level filter integration
    - **Property 7: Saved filter apply round-trip** — applying a saved filter sets FilterValues to match the saved filter's stored values
    - **Validates: Requirements 5.4**
    - **Property 8: Debounce timing** — text-search and async-search values are not propagated before 300ms
    - **Validates: Requirements 2.6**
    - **Property 9: hasActiveFilters consistency** — `hasActiveFilters` is `true` iff at least one key in FilterValues has a non-empty value
    - **Validates: Requirements 2.1**
    - **Property 10: Async-search minimum character threshold** — loadOptions is not called when query has fewer than 2 characters
    - **Validates: Requirements 8.1**

- [x] 10. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties using fast-check
- Unit tests validate specific examples and edge cases using Vitest
- Existing `SaveFilterDialog` and `SavedFilterDropdown` components are reused as-is
