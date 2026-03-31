# Requirements Document

## Introduction

The Nexus 2.0 frontend has multiple list pages (Stories, Sprints, Members, Departments, Backlog, Kanban Board, Admin Billing, Admin Organizations, Invites, Projects, Search) that each implement filtering in ad-hoc, inconsistent ways. Some use inline `<select>` dropdowns, some use a `BoardFilters` component, some use toggle-pill multi-selects, some have no filtering at all, and saved-filter support is bolted on only to board pages. This feature introduces a single, standardized `ListFilter` component and supporting hook (`useListFilters`) that all list pages adopt, replacing every bespoke filter implementation with a consistent, declarative, accessible pattern — following the same shared-component philosophy as the existing `DataTable`.

## Glossary

- **ListFilter**: The new shared React component that renders a configurable filter bar for any list page.
- **FilterConfig**: A declarative schema object that describes the available filters for a given page (field name, label, type, options).
- **FilterValues**: A plain key-value record representing the currently active filter selections.
- **useListFilters**: A custom React hook that manages FilterValues state, URL synchronization, debouncing, and reset logic.
- **FilterFieldType**: An enumeration of supported filter input types: `select`, `multi-select`, `text-search`, `date`, `date-range`, `async-search`.
- **SavedFilter**: An existing entity (already persisted via WorkService API) that stores a named set of FilterValues for reuse.
- **DataTable**: The existing shared table component used across all list pages.
- **ListPage**: Any page in the Nexus 2.0 SPA that displays a collection of items in a table or board layout and supports filtering.

## Requirements

### Requirement 1: Declarative Filter Configuration

**User Story:** As a developer, I want to declare the filters for a page via a simple configuration array, so that adding or removing filters requires no custom UI code.

#### Acceptance Criteria

1. THE ListFilter component SHALL accept a `filters` prop of type `FilterConfig[]` that declaratively describes each filter field.
2. WHEN a FilterConfig entry has `type` set to `select`, THE ListFilter component SHALL render a single-value dropdown with the provided `options`.
3. WHEN a FilterConfig entry has `type` set to `multi-select`, THE ListFilter component SHALL render a multi-value selector that allows zero or more values to be toggled.
4. WHEN a FilterConfig entry has `type` set to `text-search`, THE ListFilter component SHALL render a text input with a search icon.
5. WHEN a FilterConfig entry has `type` set to `date`, THE ListFilter component SHALL render a single date picker input.
6. WHEN a FilterConfig entry has `type` set to `date-range`, THE ListFilter component SHALL render paired "from" and "to" date inputs.
7. WHEN a FilterConfig entry has `type` set to `async-search`, THE ListFilter component SHALL render a text input that fetches options asynchronously via a provided `loadOptions` callback.
8. THE FilterConfig interface SHALL include the fields: `key` (string), `label` (string), `type` (FilterFieldType), `options` (optional static array), `loadOptions` (optional async callback), and `placeholder` (optional string).

### Requirement 2: Centralized Filter State Management

**User Story:** As a developer, I want a single hook to manage all filter state for a page, so that filter logic is not duplicated across every list page.

#### Acceptance Criteria

1. THE useListFilters hook SHALL accept an initial FilterConfig array and return the current FilterValues, an `updateFilter` function, a `clearFilters` function, and a `hasActiveFilters` boolean.
2. WHEN `updateFilter` is called with a key and value, THE useListFilters hook SHALL update the corresponding entry in FilterValues.
3. WHEN `updateFilter` is called with an empty or undefined value, THE useListFilters hook SHALL remove that key from FilterValues.
4. WHEN `clearFilters` is called, THE useListFilters hook SHALL reset FilterValues to an empty record.
5. WHEN any filter value changes, THE useListFilters hook SHALL reset the page number to 1 (via a provided `onPageReset` callback or by returning a `pageResetSignal`).
6. WHEN a `text-search` or `async-search` filter value changes, THE useListFilters hook SHALL debounce the value update by 300 milliseconds before propagating it to FilterValues.

### Requirement 3: URL Synchronization

**User Story:** As a user, I want my active filters to be reflected in the browser URL, so that I can share or bookmark a filtered view.

#### Acceptance Criteria

1. WHEN FilterValues change, THE useListFilters hook SHALL serialize the active filters into URL search parameters.
2. WHEN the page loads with filter-related search parameters in the URL, THE useListFilters hook SHALL initialize FilterValues from those parameters.
3. THE useListFilters hook SHALL accept an `syncToUrl` option (default `true`) that enables or disables URL synchronization.
4. WHEN `syncToUrl` is `false`, THE useListFilters hook SHALL manage FilterValues in local state only without modifying the URL.

### Requirement 4: Consistent Visual Layout

**User Story:** As a user, I want all list pages to present filters in the same visual style, so that the application feels cohesive and predictable.

#### Acceptance Criteria

1. THE ListFilter component SHALL render a collapsible filter bar that can be toggled open and closed via a "Filters" button.
2. WHILE the filter bar is open, THE ListFilter component SHALL display all configured filter fields in a responsive grid layout (adapting from 1 column on small screens to 4 columns on large screens).
3. WHEN at least one filter is active, THE ListFilter component SHALL display a badge count on the "Filters" toggle button indicating the number of active filters.
4. WHEN at least one filter is active and the filter bar is open, THE ListFilter component SHALL display a "Clear all" button that invokes `clearFilters`.
5. THE ListFilter component SHALL use the same Tailwind CSS design tokens (border-input, bg-background, text-foreground, bg-card, border-border) as the existing DataTable component.

### Requirement 5: Saved Filters Integration

**User Story:** As a user, I want to save and load named filter presets from any list page, so that I can quickly switch between frequently used filter combinations.

#### Acceptance Criteria

1. THE ListFilter component SHALL accept an optional `enableSavedFilters` prop (default `false`).
2. WHEN `enableSavedFilters` is `true` and at least one filter is active, THE ListFilter component SHALL display a "Save Filter" action that opens the existing SaveFilterDialog with the current FilterValues.
3. WHEN `enableSavedFilters` is `true`, THE ListFilter component SHALL display the existing SavedFilterDropdown that loads saved filters and applies them to FilterValues on selection.
4. WHEN a saved filter is applied, THE ListFilter component SHALL update FilterValues to match the saved filter's stored values.

### Requirement 6: Accessibility

**User Story:** As a user who relies on assistive technology, I want the filter controls to be fully keyboard-navigable and screen-reader-friendly, so that I can use filters without a mouse.

#### Acceptance Criteria

1. THE ListFilter component SHALL associate each filter input with a visible `<label>` element using matching `htmlFor` and `id` attributes.
2. THE ListFilter component SHALL ensure the "Filters" toggle button has an `aria-expanded` attribute reflecting the open/closed state of the filter bar.
3. THE ListFilter component SHALL ensure the collapsible filter region has an `aria-label` of "Filter options".
4. WHEN a multi-select filter is rendered, THE ListFilter component SHALL use `role="group"` with an `aria-label` matching the filter label, and each toggle option SHALL use `aria-pressed` to indicate selection state.
5. THE ListFilter component SHALL ensure all interactive elements are reachable and operable via keyboard Tab and Enter/Space keys.

### Requirement 7: Adoption Across Existing List Pages

**User Story:** As a developer, I want every existing list page to use the new ListFilter component, so that there is a single filter pattern in the codebase.

#### Acceptance Criteria

1. WHEN the StoryListPage is rendered, THE StoryListPage SHALL use the ListFilter component with FilterConfig entries for project, status, priority, department, assignee, and date range filters.
2. WHEN the SprintListPage is rendered, THE SprintListPage SHALL use the ListFilter component with a FilterConfig entry for project filter.
3. WHEN the MemberListPage is rendered, THE MemberListPage SHALL use the ListFilter component with FilterConfig entries for department, role, status, and availability filters.
4. WHEN the BacklogPage is rendered, THE BacklogPage SHALL use the ListFilter component with FilterConfig entries for project and priority filters, with saved filters enabled.
5. WHEN the KanbanBoardPage is rendered, THE KanbanBoardPage SHALL use the ListFilter component with FilterConfig entries for project and priority filters, with saved filters enabled.
6. WHEN the PlatformAdminBillingPage is rendered, THE PlatformAdminBillingPage SHALL use the ListFilter component with FilterConfig entries for status and organization search filters.
7. WHEN the DepartmentListPage is rendered, THE DepartmentListPage SHALL use the ListFilter component with a FilterConfig entry for status filter.
8. WHEN the InviteManagementPage is rendered, THE InviteManagementPage SHALL use the ListFilter component with a FilterConfig entry for status filter.
9. WHEN all list pages have adopted the ListFilter component, THE codebase SHALL no longer contain the `BoardFilters` component or any inline ad-hoc filter markup in list pages.

### Requirement 8: Async Option Loading

**User Story:** As a developer, I want filters that depend on API data (like project lists or member search) to load their options asynchronously, so that the filter component does not require pages to pre-fetch option data.

#### Acceptance Criteria

1. WHEN a FilterConfig entry has `type` set to `async-search` and the user types at least 2 characters, THE ListFilter component SHALL call the `loadOptions` callback with the query string and display the returned options.
2. WHILE the `loadOptions` callback is pending, THE ListFilter component SHALL display a loading indicator within the filter field.
3. IF the `loadOptions` callback fails, THEN THE ListFilter component SHALL display the filter field in an empty state without crashing.
4. WHEN a FilterConfig entry has `type` set to `select` and an `loadOptions` callback is provided instead of static `options`, THE ListFilter component SHALL call `loadOptions` once on mount to populate the dropdown options.
