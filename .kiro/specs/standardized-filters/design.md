# Design Document: Standardized Filters

## Overview

This feature replaces all ad-hoc, inconsistent filter implementations across Nexus 2.0 list pages with a single shared ListFilter component and useListFilters hook. Today, each page rolls its own filter UI: StoryListPage has a hand-built collapsible panel with inline multi-select pills and an async assignee search; MemberListPage uses bare select dropdowns; SprintListPage has a single project select; board pages (BacklogPage, KanbanBoardPage) use the BoardFilters component with SaveFilterDialog/SavedFilterDropdown; PlatformAdminBillingPage has inline status select plus text search; and DepartmentListPage/InviteManagementPage have no filter UI at all (or minimal inline markup).

The new system follows the same shared-component philosophy as the existing DataTable: a declarative, configuration-driven component that pages consume by passing a FilterConfig array. The useListFilters hook centralizes state management, URL synchronization, debouncing, and pagination reset, eliminating duplicated logic across pages.

### Design Decisions

1. Configuration-driven rendering - Each page declares its filters as a FilterConfig array. The ListFilter component maps over this array and renders the appropriate input type. Adding or removing a filter is a one-line config change, not a UI code change.
2. Hook plus Component separation - useListFilters owns all state logic (values, URL sync, debounce, reset). ListFilter is a presentation component that receives values and callbacks. This keeps the hook testable without DOM and the component testable without complex state setup.
3. Reuse existing saved filter infrastructure - The existing SaveFilterDialog and SavedFilterDropdown components are already functional. ListFilter composes them when enableSavedFilters is true.
4. URL sync via useSearchParams - React Router v6 useSearchParams is already available. Filter values serialize to query params. Multi-values use comma-separated strings. Opt-in via syncToUrl (default true).
5. Collapsible filter bar - Matches the pattern already established in StoryListPage. Standardizing this across all pages.
6. No backend changes - All existing API endpoints already accept filter parameters. The new components standardize how the frontend constructs and passes those parameters.

## Architecture

The architecture consists of a shared layer (types, hook, component) consumed by all list pages:

New files under src/frontend/src/:
- components/filters/types.ts - FilterConfig, FilterFieldType, FilterValues types
- components/filters/ListFilter.tsx - Shared filter bar component
- components/filters/FilterField.tsx - Renders individual filter field by type
- hooks/useListFilters.ts - Centralized filter state hook

Data flow:
1. Page defines FilterConfig array
2. Page calls useListFilters(configs, options) which returns filterValues, updateFilter, clearFilters, hasActiveFilters, activeFilterCount
3. Page passes these to ListFilter component
4. User interacts with filter UI, ListFilter calls updateFilter
5. Hook debounces text inputs (300ms), updates state, syncs to URL, calls onPageReset
6. Page re-fetches data with new filterValues

Each list page imports ListFilter and useListFilters, defines its FilterConfig array, and replaces its existing ad-hoc filter markup. The BoardFilters component (features/boards/components/BoardFilters.tsx) will be deleted after all pages migrate.

## Components and Interfaces

### FilterFieldType

```typescript
export type FilterFieldType =
  | 'select'
  | 'multi-select'
  | 'text-search'
  | 'date'
  | 'date-range'
  | 'async-search';
```

### FilterConfig Interface

```typescript
export interface FilterOption {
  value: string;
  label: string;
}

export interface FilterConfig {
  key: string;
  label: string;
  type: FilterFieldType;
  options?: FilterOption[];
  loadOptions?: (query: string) => Promise<FilterOption[]>;
  placeholder?: string;
}
```

### FilterValues Type

```typescript
export type FilterValues = Record<string, string | string[] | undefined>;
```

### useListFilters Hook

```typescript
interface UseListFiltersOptions {
  syncToUrl?: boolean;       // default: true
  onPageReset?: () => void;  // called when filters change to reset page to 1
}

interface UseListFiltersReturn {
  filterValues: FilterValues;
  updateFilter: (key: string, value: string | string[] | undefined) => void;
  clearFilters: () => void;
  hasActiveFilters: boolean;
  activeFilterCount: number;
}

export function useListFilters(
  configs: FilterConfig[],
  options?: UseListFiltersOptions
): UseListFiltersReturn;
```

Behavior:
- On mount, if syncToUrl is true, reads URL search params and initializes filterValues from them. Multi-select values are stored as comma-separated strings in the URL.
- updateFilter(key, value): updates the value for the given key. If value is empty or undefined, removes the key. For text-search and async-search types, the value update is debounced by 300ms using an internal setTimeout (similar to the existing useDebounce hook pattern but scoped per-key).
- After any filter change (post-debounce), calls onPageReset and (if syncToUrl) writes to URL search params.
- clearFilters(): resets all values to empty record, calls onPageReset, clears URL params.
- hasActiveFilters: true if any key in filterValues has a non-empty value.
- activeFilterCount: count of keys with non-empty values.

### ListFilter Component

```typescript
interface ListFilterProps {
  configs: FilterConfig[];
  values: FilterValues;
  onUpdateFilter: (key: string, value: string | string[] | undefined) => void;
  onClearFilters: () => void;
  hasActiveFilters: boolean;
  activeFilterCount: number;
  enableSavedFilters?: boolean;
  onApplySavedFilter?: (values: FilterValues) => void;
}

export function ListFilter(props: ListFilterProps): JSX.Element;
```

Rendering:
- A Filters toggle button with aria-expanded. When active filters exist, shows a badge with activeFilterCount.
- When open, renders a div with aria-label of Filter options containing a responsive grid (grid-cols-1 sm:grid-cols-2 lg:grid-cols-4).
- Each filter field is rendered by FilterField based on its type.
- When hasActiveFilters is true and the panel is open, shows a Clear all button.
- When enableSavedFilters is true, renders SavedFilterDropdown and (when filters are active) SaveFilterDialog in the header row.

### FilterField Component

```typescript
interface FilterFieldProps {
  config: FilterConfig;
  value: string | string[] | undefined;
  onChange: (value: string | string[] | undefined) => void;
}

export function FilterField(props: FilterFieldProps): JSX.Element;
```

Renders by type:
- select: label plus select dropdown with All label default option. If loadOptions is provided instead of options, calls it once on mount to populate.
- multi-select: label plus role group container with aria-label. Each option is a toggle button with aria-pressed.
- text-search: label plus input type text with search icon and placeholder.
- date: label plus input type date.
- date-range: Two labeled date inputs (From/To) rendered as a pair.
- async-search: label plus input type text that triggers loadOptions when 2 or more characters are typed. Shows a loading spinner while pending. Displays results in a dropdown list. On failure, shows empty state.

### Page-Specific FilterConfig Examples

StoryListPage config:

```typescript
const storyFilterConfigs: FilterConfig[] = [
  {
    key: 'projectId', label: 'Project', type: 'select',
    loadOptions: async () => {
      const res = await workApi.getProjects({ page: 1, pageSize: 100 });
      return res.data.map(p => ({ value: p.projectId, label: p.name }));
    },
  },
  {
    key: 'status', label: 'Status', type: 'multi-select',
    options: Object.values(StoryStatus).map(s => ({ value: s, label: s })),
  },
  {
    key: 'priority', label: 'Priority', type: 'multi-select',
    options: Object.values(Priority).map(p => ({ value: p, label: p })),
  },
  {
    key: 'departmentId', label: 'Department', type: 'select',
    loadOptions: async () => {
      const res = await profileApi.getDepartments();
      return res.data.map(d => ({ value: d.departmentId, label: d.name }));
    },
  },
  {
    key: 'assigneeId', label: 'Assignee', type: 'async-search',
    placeholder: 'Search assignee...',
    loadOptions: async (query) => {
      const res = await profileApi.getTeamMembers({ page: 1, pageSize: 10 });
      return res.data
        .filter(m => `${m.firstName} ${m.lastName}`.toLowerCase().includes(query.toLowerCase()))
        .map(m => ({ value: m.teamMemberId, label: `${m.firstName} ${m.lastName}` }));
    },
  },
  { key: 'dateFrom', label: 'From Date', type: 'date' },
  { key: 'dateTo', label: 'To Date', type: 'date' },
];
```

## Data Models

### Core Types (new file: components/filters/types.ts)

```typescript
export type FilterFieldType =
  | 'select'
  | 'multi-select'
  | 'text-search'
  | 'date'
  | 'date-range'
  | 'async-search';

export interface FilterOption {
  value: string;
  label: string;
}

export interface FilterConfig {
  key: string;
  label: string;
  type: FilterFieldType;
  options?: FilterOption[];
  loadOptions?: (query: string) => Promise<FilterOption[]>;
  placeholder?: string;
}

export type FilterValues = Record<string, string | string[] | undefined>;
```

### URL Serialization Format

- select: key=value (e.g. ?projectId=abc-123)
- multi-select: key=val1,val2 (e.g. ?status=Active,InProgress)
- text-search: key=value (e.g. ?search=acme)
- date: key=YYYY-MM-DD (e.g. ?dueDate=2024-06-15)
- date-range: key.from=date and key.to=date (e.g. ?dateRange.from=2024-01-01&dateRange.to=2024-06-30)
- async-search: key=value (e.g. ?assigneeId=member-456)

### Serialization/Deserialization Functions

```typescript
// Exported from useListFilters.ts for testing
export function serializeFilters(
  values: FilterValues,
  configs: FilterConfig[]
): URLSearchParams;

export function deserializeFilters(
  params: URLSearchParams,
  configs: FilterConfig[]
): FilterValues;
```

- serializeFilters: iterates over FilterValues, skips undefined/empty entries. For date-range types, splits into key.from and key.to params. For multi-select, joins array with commas.
- deserializeFilters: reads URL params, uses configs to determine types. For date-range, reads key.from/key.to and combines. For multi-select, splits comma-separated string into array.

### Interaction with Existing Types

The FilterValues record maps directly to existing API parameter types:
- StoryFilters (work.ts) - keys like projectId, status, priority, departmentId, assigneeId, dateFrom, dateTo
- MemberFilters (profile.ts) - keys like departmentId, roleName, status, availability
- BoardFilters (work.ts) - keys like projectId, priority
- SprintFilters (work.ts) - key projectId

Each page spreads filterValues into its API call, matching the existing parameter shapes. For date ranges, pages use two separate date type configs with keys dateFrom and dateTo for direct API compatibility. The date-range type is available for cases where a paired UI is preferred, and the page maps the compound value to API params in its fetch function.


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system - essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Config-to-field rendering

*For any* valid FilterConfig array, when the filter bar is open, the ListFilter component shall render exactly one filter field per config entry, and each rendered field input type shall correspond to its config type (select renders a select element, multi-select renders a role group container with toggle buttons, text-search renders a text input, date renders a date input, date-range renders two date inputs).

**Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 4.2**

### Property 2: updateFilter correctness

*For any* filter key and value, calling updateFilter(key, value) where value is non-empty shall result in filterValues[key] equaling that value, and calling updateFilter(key, undefined) or updateFilter(key, '') shall result in filterValues[key] being undefined (key removed from the record).

**Validates: Requirements 2.2, 2.3**

### Property 3: clearFilters resets all state

*For any* non-empty FilterValues record, calling clearFilters() shall result in filterValues being an empty record and hasActiveFilters being false.

**Validates: Requirements 2.4**

### Property 4: Filter change triggers page reset

*For any* filter key and non-empty value, calling updateFilter shall invoke the onPageReset callback exactly once (after debounce for text-search/async-search types, immediately for other types).

**Validates: Requirements 2.5**

### Property 5: URL serialization round-trip

*For any* valid FilterValues record and corresponding FilterConfig array, serializing the values to URL search parameters and then deserializing those parameters back shall produce a FilterValues record equivalent to the original.

**Validates: Requirements 3.1, 3.2**

### Property 6: Active filter badge count accuracy

*For any* FilterValues record, the activeFilterCount shall equal the number of keys in the record whose values are non-empty (non-undefined, non-empty-string, non-empty-array), and hasActiveFilters shall be true if and only if activeFilterCount is greater than 0.

**Validates: Requirements 4.3**

### Property 7: Saved filter apply correctness

*For any* saved filter containing a valid FilterValues JSON string, applying that saved filter shall result in filterValues matching the deserialized saved filter values.

**Validates: Requirements 5.4**

### Property 8: Label-input accessibility association

*For any* FilterConfig array, every rendered filter field shall have a label element whose htmlFor attribute matches the id attribute of the corresponding input element.

**Validates: Requirements 6.1**

### Property 9: Multi-select ARIA attributes

*For any* FilterConfig entry with type set to multi-select, the rendered container shall have role group and an aria-label matching the config label, and each toggle option within shall have an aria-pressed attribute that is true when selected and false when not selected.

**Validates: Requirements 6.4**

### Property 10: Async loadOptions failure resilience

*For any* FilterConfig entry with a loadOptions callback that rejects with an error, the FilterField component shall render in an empty/default state without throwing an unhandled exception.

**Validates: Requirements 8.3**


## Error Handling

### Async Option Loading Failures

When loadOptions rejects (network error, API error), the FilterField catches the error and renders the field in its default empty state (empty dropdown for select, no results for async-search). No toast or error message is shown - the filter simply has no options. This matches the existing pattern in BoardFilters where getProjects failures are silently caught.

### Invalid URL Parameters

When deserializeFilters encounters URL params that do not match any config key, those params are ignored (not added to FilterValues). When a multi-select URL param contains values not in the config options, those values are still included in FilterValues (the API will simply ignore unknown values). This is a permissive approach that avoids breaking bookmarked URLs when option sets change.

### Invalid Saved Filter Data

When SavedFilterDropdown applies a saved filter whose JSON is malformed, the existing try/catch in SavedFilterDropdown.handleApply shows an error toast. The ListFilter component does not need additional handling. When a saved filter contains keys not present in the current page FilterConfig array, those keys are included in FilterValues but have no rendered field. They are passed to the API call harmlessly.

### Empty/Missing Config

When ListFilter receives an empty FilterConfig array, it renders the toggle button but the panel contains no fields. The Clear all button is hidden since hasActiveFilters is false.

### Debounce Edge Cases

When a user types rapidly in a text-search field and then immediately clicks Clear all, the pending debounce timer is cancelled. clearFilters clears all values including any pending debounced value. When the component unmounts while a debounce timer is pending, the timer is cleaned up via the useEffect cleanup function to prevent state updates on unmounted components.

## Testing Strategy

### Property-Based Testing

The project already uses fast-check (v4.6.0) with Vitest for property-based tests. All property tests follow the existing pattern established in src/frontend/src/__tests__/form-schemas.test.ts.

Each correctness property from the design document maps to a single property-based test. Tests are placed in src/frontend/src/__tests__/list-filters.test.ts (for hook/logic tests) and src/frontend/src/__tests__/list-filter-component.test.tsx (for component rendering tests).

Configuration:
- Minimum 100 iterations per property test (numRuns: 100)
- Each test tagged with a comment referencing the design property
- Tag format: Feature: standardized-filters, Property N: property title

Generators needed:
- arbFilterConfig: generates random FilterConfig objects with valid types, keys, labels, and options
- arbFilterValues: generates random FilterValues records consistent with a given FilterConfig array
- arbFilterFieldType: generates random FilterFieldType values

Property test mapping:

| Property | Test File | What it tests |
|---|---|---|
| Property 1: Config-to-field rendering | list-filter-component.test.tsx | Render ListFilter with random configs, verify field count and types |
| Property 2: updateFilter correctness | list-filters.test.ts | Call updateFilter with random keys/values via renderHook, verify state |
| Property 3: clearFilters resets all | list-filters.test.ts | Set random filters then clear, verify empty state |
| Property 4: Filter change triggers page reset | list-filters.test.ts | Call updateFilter, verify onPageReset callback invoked |
| Property 5: URL serialization round-trip | list-filters.test.ts | Serialize random FilterValues, deserialize, compare |
| Property 6: Active filter count accuracy | list-filters.test.ts | Generate random FilterValues, verify count matches |
| Property 7: Saved filter apply | list-filter-component.test.tsx | Apply random saved filter JSON, verify filterValues match |
| Property 8: Label-input association | list-filter-component.test.tsx | Render random configs, verify label htmlFor matches input id |
| Property 9: Multi-select ARIA | list-filter-component.test.tsx | Render multi-select configs, verify role/aria-label/aria-pressed |
| Property 10: Async failure resilience | list-filter-component.test.tsx | Render with failing loadOptions, verify no crash |

### Unit Testing

Unit tests complement property tests for specific examples, edge cases, and integration points:

- Debounce behavior: verify 300ms delay for text-search, immediate for select (specific timing test)
- URL sync disabled: verify URL is not modified when syncToUrl is false
- Collapsible toggle: verify panel opens/closes on button click, aria-expanded toggles
- Clear all button visibility: verify shown only when filters active and panel open
- SaveFilterDialog/SavedFilterDropdown integration: verify they render when enableSavedFilters is true
- Async-search 2-char threshold: verify loadOptions not called with fewer than 2 chars
- Loading indicator: verify spinner shown while loadOptions is pending
- Select with loadOptions on mount: verify loadOptions called once on mount
- Page adoption smoke tests: one unit test per page verifying ListFilter is rendered with expected config keys (requirements 7.1-7.8)

### Test Organization

```
src/frontend/src/__tests__/
  list-filters.test.ts              # Hook logic: properties 2-6, debounce, URL sync
  list-filter-component.test.tsx    # Component rendering: properties 1, 7-10, toggle, a11y
  list-filter-adoption.test.tsx     # Page adoption smoke tests (7.1-7.8)
```
