import { describe, it, expect, vi } from 'vitest';
import * as fc from 'fast-check';
import { render, fireEvent, within, cleanup } from '@testing-library/react';
import type { FilterConfig, FilterFieldType, FilterValues } from '@/types/filters';
import { ListFilter } from '@/components/common/ListFilter';

/**
 * Feature: standardized-filters
 *
 * Property-based tests for the ListFilter component.
 */

// --- Mock saved-filter components that depend on API context ---

vi.mock('@/features/filters/components/SavedFilterDropdown', () => ({
    SavedFilterDropdown: () => <div data-testid="saved-filter-dropdown" />,
}));

vi.mock('@/features/filters/components/SaveFilterDialog', () => ({
    SaveFilterDialog: () => <div data-testid="save-filter-dialog" />,
}));

// --- Generators ---

const ALL_TYPES: FilterFieldType[] = ['select', 'multi-select', 'text-search', 'date', 'date-range'];

const arbFilterFieldType = fc.constantFrom<FilterFieldType>(...ALL_TYPES);

const arbFilterKey = fc.stringMatching(/^[a-zA-Z][a-zA-Z0-9]{0,14}$/).filter((s) => s.length > 0);

const arbFilterLabel = fc.stringMatching(/^[A-Za-z][A-Za-z0-9]{0,14}$/).filter((s) => s.length > 0);

/** Generate a FilterConfig with options for select/multi-select types */
function arbFilterConfigFull(): fc.Arbitrary<FilterConfig> {
    return fc.record({
        key: arbFilterKey,
        label: arbFilterLabel,
        type: arbFilterFieldType,
    }).map((cfg) => {
        if (cfg.type === 'select' || cfg.type === 'multi-select') {
            return {
                ...cfg,
                options: [
                    { value: 'opt1', label: 'Option 1' },
                    { value: 'opt2', label: 'Option 2' },
                ],
            };
        }
        return cfg;
    });
}

/** Generate a list of FilterConfigs with unique keys */
function arbFilterConfigs(minLength = 1, maxLength = 6): fc.Arbitrary<FilterConfig[]> {
    return fc
        .uniqueArray(arbFilterConfigFull(), { minLength, maxLength, selector: (c) => c.key })
        .filter((arr) => arr.length >= minLength);
}

/** Generate a non-empty filter value string */
const arbFilterValue = fc.stringMatching(/^[a-zA-Z0-9]{1,10}$/).filter((s) => s.length > 0);

/**
 * Generate a FilterValues record where a random subset of keys have non-empty values.
 * Returns both the configs and the values for testing.
 */
function arbConfigsAndValues(
    minConfigs = 1,
    maxConfigs = 6
): fc.Arbitrary<{ configs: FilterConfig[]; values: FilterValues; expectedCount: number }> {
    return arbFilterConfigs(minConfigs, maxConfigs).chain((configs) => {
        // For each config, randomly decide if it has a value
        const boolArbs = configs.map(() => fc.boolean());
        return fc.tuple(...boolArbs).chain((hasValues) => {
            const valueArbs: fc.Arbitrary<string | string[] | undefined>[] = configs.map((cfg, i) => {
                if (!hasValues[i]) return fc.constant(undefined);
                if (cfg.type === 'multi-select') {
                    return fc.uniqueArray(arbFilterValue, { minLength: 1, maxLength: 3 }).map(
                        (arr) => arr as string[]
                    );
                }
                return arbFilterValue;
            });
            return fc.tuple(...valueArbs).map((vals) => {
                const values: FilterValues = {};
                let expectedCount = 0;
                configs.forEach((cfg, i) => {
                    const v = vals[i];
                    if (v !== undefined) {
                        values[cfg.key] = v;
                        expectedCount++;
                    }
                });
                return { configs, values, expectedCount };
            });
        });
    });
}

// --- Property Tests ---

describe('ListFilter – Property Tests', () => {
    /**
     * Feature: standardized-filters, Property 5: Active filter count accuracy
     *
     * Badge count equals the number of non-empty keys in FilterValues.
     *
     * **Validates: Requirements 4.3**
     */
    describe('Property 5: Active filter count accuracy', () => {
        it('badge count matches the number of non-empty keys in FilterValues', () => {
            fc.assert(
                fc.property(
                    arbConfigsAndValues(1, 6),
                    ({ configs, values, expectedCount }) => {
                        cleanup();
                        const { container } = render(
                            <ListFilter
                                configs={configs}
                                values={values}
                                onUpdateFilter={vi.fn()}
                                onClearFilters={vi.fn()}
                                hasActiveFilters={expectedCount > 0}
                                activeFilterCount={expectedCount}
                            />
                        );

                        if (expectedCount > 0) {
                            // The badge should be present and show the correct count
                            const badge = container.querySelector('span.rounded-full');
                            expect(badge).not.toBeNull();
                            expect(badge!.textContent).toBe(String(expectedCount));
                        } else {
                            // No badge should be rendered when count is 0
                            const badge = container.querySelector('span.rounded-full');
                            expect(badge).toBeNull();
                        }
                    }
                ),
                { numRuns: 100 }
            );
        });
    });

    /**
     * Feature: standardized-filters, Property 6: FilterConfig → rendered fields
     *
     * For any valid FilterConfig array, ListFilter renders exactly one
     * FilterField per config entry.
     *
     * **Validates: Requirements 1.1**
     */
    describe('Property 6: FilterConfig → rendered fields', () => {
        it('renders exactly one filter field per config entry when open', () => {
            fc.assert(
                fc.property(
                    arbFilterConfigs(1, 6),
                    (configs) => {
                        cleanup();
                        const { container } = render(
                            <ListFilter
                                configs={configs}
                                values={{}}
                                onUpdateFilter={vi.fn()}
                                onClearFilters={vi.fn()}
                                hasActiveFilters={false}
                                activeFilterCount={0}
                            />
                        );

                        // Click the "Filters" button to open the filter bar
                        const filtersButton = within(container).getByRole('button', { name: /Filters/i });
                        fireEvent.click(filtersButton);

                        // The filter options region should exist
                        const filterRegion = within(container).getByLabelText('Filter options');
                        expect(filterRegion).toBeTruthy();

                        // Each FilterField renders a label with a for attribute matching filter-{key}
                        for (const config of configs) {
                            const label = filterRegion.querySelector(`label[for="filter-${config.key}"]`);
                            expect(label).not.toBeNull();
                        }

                        // Count total labels inside the filter region to match config count
                        const allLabels = filterRegion.querySelectorAll('label');
                        // Each config gets exactly one <label> from FilterField
                        expect(allLabels.length).toBe(configs.length);
                    }
                ),
                { numRuns: 100 }
            );
        });
    });
});


// --- Unit Tests for FilterField ---

import { screen, waitFor } from '@testing-library/react';
import { FilterField } from '@/components/common/FilterField';

describe('FilterField – Unit Tests', () => {
    /**
     * Each filter type renders the correct input element.
     * Validates: Requirements 1.2, 1.3, 1.4, 1.5, 1.6
     */
    describe('renders correct input element per type', () => {
        it('select renders a <select> with an "All" default option', () => {
            cleanup();
            const config: FilterConfig = {
                key: 'status',
                label: 'Status',
                type: 'select',
                options: [
                    { value: 'active', label: 'Active' },
                    { value: 'inactive', label: 'Inactive' },
                ],
            };
            render(<FilterField config={config} value={undefined} onChange={vi.fn()} />);
            const select = document.getElementById('filter-status') as HTMLSelectElement;
            expect(select).not.toBeNull();
            expect(select.tagName).toBe('SELECT');
            // First option should be "All"
            const firstOption = select.querySelector('option');
            expect(firstOption?.textContent).toBe('All');
            expect(firstOption?.value).toBe('');
        });

        it('multi-select renders a div with role="group" and aria-label', () => {
            cleanup();
            const config: FilterConfig = {
                key: 'priority',
                label: 'Priority',
                type: 'multi-select',
                options: [
                    { value: 'high', label: 'High' },
                    { value: 'low', label: 'Low' },
                ],
            };
            render(<FilterField config={config} value={undefined} onChange={vi.fn()} />);
            const group = document.getElementById('filter-priority');
            expect(group).not.toBeNull();
            expect(group?.getAttribute('role')).toBe('group');
            expect(group?.getAttribute('aria-label')).toBe('Priority');
        });

        it('text-search renders an <input type="text">', () => {
            cleanup();
            const config: FilterConfig = {
                key: 'search',
                label: 'Search',
                type: 'text-search',
                placeholder: 'Search…',
            };
            render(<FilterField config={config} value={undefined} onChange={vi.fn()} />);
            const input = document.getElementById('filter-search') as HTMLInputElement;
            expect(input).not.toBeNull();
            expect(input.tagName).toBe('INPUT');
            expect(input.type).toBe('text');
        });

        it('date renders an <input type="date">', () => {
            cleanup();
            const config: FilterConfig = {
                key: 'dueDate',
                label: 'Due Date',
                type: 'date',
            };
            render(<FilterField config={config} value={undefined} onChange={vi.fn()} />);
            const input = document.getElementById('filter-dueDate') as HTMLInputElement;
            expect(input).not.toBeNull();
            expect(input.type).toBe('date');
        });

        it('date-range renders two <input type="date"> elements', () => {
            cleanup();
            const config: FilterConfig = {
                key: 'dateRange',
                label: 'Date Range',
                type: 'date-range',
            };
            render(<FilterField config={config} value={undefined} onChange={vi.fn()} />);
            const fromInput = document.getElementById('filter-dateRange') as HTMLInputElement;
            const toInput = document.getElementById('filter-dateRange-to') as HTMLInputElement;
            expect(fromInput).not.toBeNull();
            expect(fromInput.type).toBe('date');
            expect(toInput).not.toBeNull();
            expect(toInput.type).toBe('date');
        });

        it('async-search renders an <input type="text">', () => {
            cleanup();
            const config: FilterConfig = {
                key: 'assignee',
                label: 'Assignee',
                type: 'async-search',
                placeholder: 'Search assignee…',
                loadOptions: vi.fn().mockResolvedValue([]),
            };
            render(<FilterField config={config} value={undefined} onChange={vi.fn()} />);
            const input = document.getElementById('filter-assignee') as HTMLInputElement;
            expect(input).not.toBeNull();
            expect(input.tagName).toBe('INPUT');
            expect(input.type).toBe('text');
        });
    });

    /**
     * async-search shows loading indicator while loadOptions is pending.
     * Validates: Requirements 8.1, 8.2
     */
    it('async-search shows loading indicator while loadOptions is pending', async () => {
        cleanup();
        // loadOptions that never resolves — keeps loading state active
        const loadOptions = vi.fn().mockReturnValue(new Promise(() => { }));
        const config: FilterConfig = {
            key: 'assignee',
            label: 'Assignee',
            type: 'async-search',
            placeholder: 'Search assignee…',
            loadOptions,
        };
        const { container } = render(
            <FilterField config={config} value={undefined} onChange={vi.fn()} />
        );
        const input = document.getElementById('filter-assignee') as HTMLInputElement;
        // Type 2+ characters to trigger loadOptions
        fireEvent.change(input, { target: { value: 'Jo' } });

        // Wait for debounce (300ms) + a bit of buffer
        await waitFor(
            () => {
                expect(loadOptions).toHaveBeenCalledWith('Jo');
            },
            { timeout: 1000 }
        );

        // The Loader2 spinner should be rendered (it's an svg with animate-spin class)
        const spinner = container.querySelector('.animate-spin');
        expect(spinner).not.toBeNull();
    });

    /**
     * async-search handles loadOptions failure without crashing.
     * Validates: Requirements 8.3
     */
    it('async-search handles loadOptions failure without crashing', async () => {
        cleanup();
        const loadOptions = vi.fn().mockRejectedValue(new Error('Network error'));
        const config: FilterConfig = {
            key: 'assignee',
            label: 'Assignee',
            type: 'async-search',
            placeholder: 'Search assignee…',
            loadOptions,
        };
        // Should not throw
        const { container } = render(
            <FilterField config={config} value={undefined} onChange={vi.fn()} />
        );
        const input = document.getElementById('filter-assignee') as HTMLInputElement;
        fireEvent.change(input, { target: { value: 'fail' } });

        await waitFor(
            () => {
                expect(loadOptions).toHaveBeenCalledWith('fail');
            },
            { timeout: 1000 }
        );

        // Component should still be in the DOM and not crashed
        expect(input).toBeTruthy();
        expect(container.querySelector('input')).not.toBeNull();
    });

    /**
     * select with loadOptions calls it once on mount.
     * Validates: Requirements 8.4
     */
    it('select with loadOptions calls it once on mount', async () => {
        cleanup();
        const loadOptions = vi.fn().mockResolvedValue([
            { value: 'p1', label: 'Project 1' },
            { value: 'p2', label: 'Project 2' },
        ]);
        const config: FilterConfig = {
            key: 'project',
            label: 'Project',
            type: 'select',
            loadOptions,
        };
        render(<FilterField config={config} value={undefined} onChange={vi.fn()} />);

        await waitFor(() => {
            expect(loadOptions).toHaveBeenCalledTimes(1);
        });
        // Called with empty string on mount
        expect(loadOptions).toHaveBeenCalledWith('');
    });

    /**
     * multi-select uses aria-pressed and role="group".
     * Validates: Requirements 6.4
     */
    describe('multi-select accessibility', () => {
        it('each option button has aria-pressed reflecting selection state', () => {
            cleanup();
            const config: FilterConfig = {
                key: 'tags',
                label: 'Tags',
                type: 'multi-select',
                options: [
                    { value: 'bug', label: 'Bug' },
                    { value: 'feature', label: 'Feature' },
                    { value: 'docs', label: 'Docs' },
                ],
            };
            // 'bug' is selected, others are not
            render(<FilterField config={config} value={['bug']} onChange={vi.fn()} />);

            const group = document.getElementById('filter-tags')!;
            expect(group.getAttribute('role')).toBe('group');
            expect(group.getAttribute('aria-label')).toBe('Tags');

            const buttons = group.querySelectorAll('button');
            expect(buttons.length).toBe(3);

            // Bug is selected
            expect(buttons[0].getAttribute('aria-pressed')).toBe('true');
            // Feature and Docs are not selected
            expect(buttons[1].getAttribute('aria-pressed')).toBe('false');
            expect(buttons[2].getAttribute('aria-pressed')).toBe('false');
        });

        it('toggling a multi-select option updates aria-pressed', () => {
            cleanup();
            const onChange = vi.fn();
            const config: FilterConfig = {
                key: 'tags',
                label: 'Tags',
                type: 'multi-select',
                options: [
                    { value: 'bug', label: 'Bug' },
                    { value: 'feature', label: 'Feature' },
                ],
            };
            render(<FilterField config={config} value={[]} onChange={onChange} />);

            const group = document.getElementById('filter-tags')!;
            const buttons = group.querySelectorAll('button');

            // Click "Bug" to select it
            fireEvent.click(buttons[0]);
            expect(onChange).toHaveBeenCalledWith(['bug']);
        });
    });
});


// --- Unit Tests for ListFilter ---

describe('ListFilter – Unit Tests', () => {
    const baseConfigs: FilterConfig[] = [
        {
            key: 'status',
            label: 'Status',
            type: 'select',
            options: [
                { value: 'active', label: 'Active' },
                { value: 'inactive', label: 'Inactive' },
            ],
        },
        {
            key: 'priority',
            label: 'Priority',
            type: 'multi-select',
            options: [
                { value: 'high', label: 'High' },
                { value: 'low', label: 'Low' },
            ],
        },
    ];

    /**
     * Toggle button has correct aria-expanded attribute.
     * Validates: Requirements 4.1, 6.2
     */
    describe('toggle button aria-expanded', () => {
        it('has aria-expanded="false" when the filter panel is closed', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{}}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={false}
                    activeFilterCount={0}
                />
            );
            const btn = within(container).getByRole('button', { name: /Filters/i });
            expect(btn.getAttribute('aria-expanded')).toBe('false');
        });

        it('has aria-expanded="true" after clicking the toggle button', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{}}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={false}
                    activeFilterCount={0}
                />
            );
            const btn = within(container).getByRole('button', { name: /Filters/i });
            fireEvent.click(btn);
            expect(btn.getAttribute('aria-expanded')).toBe('true');
        });

        it('toggles back to aria-expanded="false" on second click', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{}}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={false}
                    activeFilterCount={0}
                />
            );
            const btn = within(container).getByRole('button', { name: /Filters/i });
            fireEvent.click(btn);
            fireEvent.click(btn);
            expect(btn.getAttribute('aria-expanded')).toBe('false');
        });
    });

    /**
     * Filter region has aria-label="Filter options".
     * Validates: Requirements 6.3
     */
    describe('filter region aria-label', () => {
        it('renders a region with aria-label="Filter options" when open', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{}}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={false}
                    activeFilterCount={0}
                />
            );
            const btn = within(container).getByRole('button', { name: /Filters/i });
            fireEvent.click(btn);
            const region = within(container).getByLabelText('Filter options');
            expect(region).toBeTruthy();
        });

        it('does not render the filter region when closed', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{}}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={false}
                    activeFilterCount={0}
                />
            );
            const region = container.querySelector('[aria-label="Filter options"]');
            expect(region).toBeNull();
        });
    });

    /**
     * "Clear all" button appears only when filters are active AND panel is open.
     * Validates: Requirements 4.4
     */
    describe('"Clear all" button visibility', () => {
        it('does not show "Clear all" when no filters are active (panel open)', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{}}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={false}
                    activeFilterCount={0}
                />
            );
            const btn = within(container).getByRole('button', { name: /Filters/i });
            fireEvent.click(btn);
            const clearBtn = container.querySelector('button');
            const allButtons = Array.from(container.querySelectorAll('button'));
            const clearAll = allButtons.find((b) => b.textContent?.includes('Clear all'));
            expect(clearAll).toBeUndefined();
        });

        it('does not show "Clear all" when filters are active but panel is closed', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{ status: 'active' }}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={true}
                    activeFilterCount={1}
                />
            );
            // Panel is closed by default
            const allButtons = Array.from(container.querySelectorAll('button'));
            const clearAll = allButtons.find((b) => b.textContent?.includes('Clear all'));
            expect(clearAll).toBeUndefined();
        });

        it('shows "Clear all" when filters are active and panel is open', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{ status: 'active' }}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={true}
                    activeFilterCount={1}
                />
            );
            const btn = within(container).getByRole('button', { name: /Filters/i });
            fireEvent.click(btn);
            const allButtons = Array.from(container.querySelectorAll('button'));
            const clearAll = allButtons.find((b) => b.textContent?.includes('Clear all'));
            expect(clearAll).toBeDefined();
        });

        it('calls onClearFilters when "Clear all" is clicked', () => {
            cleanup();
            const onClearFilters = vi.fn();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{ status: 'active' }}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={onClearFilters}
                    hasActiveFilters={true}
                    activeFilterCount={1}
                />
            );
            const btn = within(container).getByRole('button', { name: /Filters/i });
            fireEvent.click(btn);
            const allButtons = Array.from(container.querySelectorAll('button'));
            const clearAll = allButtons.find((b) => b.textContent?.includes('Clear all'));
            fireEvent.click(clearAll!);
            expect(onClearFilters).toHaveBeenCalledTimes(1);
        });
    });

    /**
     * SavedFilterDropdown and SaveFilterDialog render when enableSavedFilters is true.
     * Validates: Requirements 5.2, 5.3
     */
    describe('saved filters integration', () => {
        it('does not render SavedFilterDropdown or SaveFilterDialog when enableSavedFilters is false', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{}}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={false}
                    activeFilterCount={0}
                    enableSavedFilters={false}
                />
            );
            expect(container.querySelector('[data-testid="saved-filter-dropdown"]')).toBeNull();
            expect(container.querySelector('[data-testid="save-filter-dialog"]')).toBeNull();
        });

        it('renders SavedFilterDropdown and SaveFilterDialog when enableSavedFilters is true', () => {
            cleanup();
            const { container } = render(
                <ListFilter
                    configs={baseConfigs}
                    values={{}}
                    onUpdateFilter={vi.fn()}
                    onClearFilters={vi.fn()}
                    hasActiveFilters={false}
                    activeFilterCount={0}
                    enableSavedFilters={true}
                />
            );
            expect(container.querySelector('[data-testid="saved-filter-dropdown"]')).not.toBeNull();
            expect(container.querySelector('[data-testid="save-filter-dialog"]')).not.toBeNull();
        });
    });
});


// --- Integration Property Tests ---

describe('FilterField – Integration Property Tests', () => {
    /**
     * Feature: standardized-filters, Property 10: Async-search minimum character threshold
     *
     * loadOptions is not called when query has fewer than 2 characters.
     *
     * **Validates: Requirements 8.1**
     */
    describe('Property 10: Async-search minimum character threshold', () => {
        it('loadOptions is NOT called when typing fewer than 2 characters', () => {
            /** Generate a short query string of 0 or 1 characters */
            const arbShortQuery = fc.string({ minLength: 0, maxLength: 1 });

            fc.assert(
                fc.property(arbShortQuery, (shortQuery) => {
                    cleanup();
                    vi.useFakeTimers();

                    const loadOptions = vi.fn().mockResolvedValue([]);
                    const config: FilterConfig = {
                        key: 'assignee',
                        label: 'Assignee',
                        type: 'async-search',
                        placeholder: 'Search assignee…',
                        loadOptions,
                    };

                    render(
                        <FilterField config={config} value={undefined} onChange={vi.fn()} />
                    );

                    const input = document.getElementById('filter-assignee') as HTMLInputElement;
                    fireEvent.change(input, { target: { value: shortQuery } });

                    // Advance past the debounce window to ensure any pending call would fire
                    vi.advanceTimersByTime(500);

                    // loadOptions should NOT have been called for queries < 2 chars
                    expect(loadOptions).not.toHaveBeenCalled();

                    vi.useRealTimers();
                }),
                { numRuns: 100 }
            );
        });
    });
});
